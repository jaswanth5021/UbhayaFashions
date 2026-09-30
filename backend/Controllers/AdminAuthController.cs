using backend.Data;
using backend.DTOs;
using backend.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace backend.Controllers;

[ApiController]
[Route("api/admin/auth")]
public class AdminAuthController(
    ApplicationDbContext db,
    IConfiguration config) : ControllerBase
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