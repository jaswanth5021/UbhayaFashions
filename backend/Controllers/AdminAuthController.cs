using backend.Data;
using backend.DTOs;
using backend.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;

namespace backend.Controllers;

[ApiController]
[Route("api/admin/auth")]
public class AdminAuthController(
    ApplicationDbContext db,
    IConfiguration config,
    ILogger<AdminAuthController> logger) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login(AdminLoginRequest request)
    {
        var email = request.Email?
            .Trim()
            .ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(
                "Email and password are required."
            );
        }

        var admin = await db.AdminUsers
            .FirstOrDefaultAsync(x =>
                x.Email == email &&
                x.IsActive);

        if (admin is null ||
            !BCrypt.Net.BCrypt.Verify(
                request.Password,
                admin.PasswordHash))
        {
            return Unauthorized(
                "Invalid admin email or password."
            );
        }

        return Ok(
            new AdminLoginResponse(
                Token(admin),
                admin.Id,
                admin.Name,
                admin.Email
            )
        );
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(AdminChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 12)
            return BadRequest("Admin password must contain at least 12 characters.");
        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            return BadRequest("Current password is required.");
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(adminId, out var id)) return Unauthorized();
        var admin = await db.AdminUsers.FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        if (admin is null || !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, admin.PasswordHash))
            return BadRequest("Current password is incorrect.");
        if (BCrypt.Net.BCrypt.Verify(request.NewPassword, admin.PasswordHash))
            return BadRequest("Choose a password you have not used before.");
        admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(AdminForgotPasswordRequest request)
    {
        const string messageText = "If an active admin account matches that email, a reset link has been sent.";
        var smtp = config.GetSection("Smtp");
        var host = smtp["Host"];
        var from = smtp["From"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
        {
            logger.LogError("Admin password reset email is not configured.");
            return Ok(new { message = messageText });
        }

        var email = request.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email)) return Ok(new { message = messageText });
        var admin = await db.AdminUsers.FirstOrDefaultAsync(x => x.Email == email && x.IsActive);
        if (admin is null) return Ok(new { message = messageText });

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        admin.PasswordResetTokenHash = HashResetToken(token);
        admin.PasswordResetExpiresUtc = DateTime.UtcNow.AddHours(1);
        await db.SaveChangesAsync();

        var frontendUrl = config["FrontendUrl"]?.TrimEnd('/') ?? "https://localhost:5001";
        var resetUrl = $"{frontendUrl}/Admin/ResetPassword?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
        try
        {
            using var mail = new MailMessage(from, email)
            {
                Subject = "Reset your Ubhaya Fashions admin password",
                Body = $"Use this one-time link to reset your admin password. It expires in one hour:\n\n{resetUrl}\n\nIf you did not request this, you can ignore this email.",
                IsBodyHtml = false
            };
            var port = int.TryParse(smtp["Port"], out var configuredPort) ? configuredPort : 587;
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = bool.TryParse(smtp["EnableSsl"], out var ssl) ? ssl : true,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };
            var username = smtp["Username"];
            if (!string.IsNullOrWhiteSpace(username))
                client.Credentials = new NetworkCredential(username, smtp["Password"]);
            await client.SendMailAsync(mail);
        }
        catch (Exception ex)
        {
            admin.PasswordResetTokenHash = null;
            admin.PasswordResetExpiresUtc = null;
            await db.SaveChangesAsync();
            logger.LogWarning(ex, "Could not send an admin password reset email.");
        }

        return Ok(new { message = messageText });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(AdminResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 12)
            return BadRequest("Admin password must contain at least 12 characters.");
        var email = request.Email?.Trim().ToLowerInvariant();
        var admin = await db.AdminUsers.FirstOrDefaultAsync(x => x.Email == email && x.IsActive);
        var tokenHash = HashResetToken(request.Token ?? "");
        if (admin is null || string.IsNullOrWhiteSpace(admin.PasswordResetTokenHash) ||
            admin.PasswordResetExpiresUtc <= DateTime.UtcNow ||
            !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(admin.PasswordResetTokenHash),
                Encoding.UTF8.GetBytes(tokenHash)))
            return BadRequest("This reset link is invalid or expired. Request a new one.");

        admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        admin.PasswordResetTokenHash = null;
        admin.PasswordResetExpiresUtc = null;
        await db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    private static string HashResetToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private string Token(AdminUser admin)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                config["Jwt:Key"]!
            )
        );

        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                admin.Id.ToString()
            ),

            new Claim(
                ClaimTypes.Name,
                admin.Name
            ),

            new Claim(
                ClaimTypes.Email,
                admin.Email
            ),

            new Claim(
                ClaimTypes.Role,
                "Admin"
            )
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                int.TryParse(
                    config["Jwt:Minutes"],
                    out var minutes
                )
                    ? minutes
                    : 120
            ),
            signingCredentials: new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            )
        );

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}
