using backend.Data;
using backend.DTOs;
using backend.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Mail;

namespace backend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    ApplicationDbContext db,
    IConfiguration config) : ControllerBase
{
    // =====================================================
    // SIGNUP
    // =====================================================

    [HttpPost("signup/request-verification")]
    public async Task<IActionResult> SignUp(SignUpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("Name and email are required.");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            return BadRequest("Password must contain at least 6 characters.");
        if (request.Age < 18 || request.Age > 120)
            return BadRequest("Age must be between 18 and 120.");

        var email = request.Email.Trim().ToLowerInvariant();
        try { _ = new MailAddress(email); }
        catch (FormatException) { return BadRequest("Enter a valid email address."); }
        var mobile = request.Mobile?.Trim() ?? "";
        var mobileDigits = NormalizePhone(mobile);
        if (mobileDigits.Length is < 7 or > 15) return BadRequest("Enter a valid mobile number.");
        if (await db.Customers.AnyAsync(x => x.Email == email))
            return Conflict("An account with this email already exists. Please login.");
        if (await db.Customers.AnyAsync(x => x.Mobile != null &&
            x.Mobile.Replace("+", "").Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace(".", "") == mobileDigits))
            return Conflict("An account with this mobile number already exists. Please login.");

        var pending = await db.PendingSignups.FirstOrDefaultAsync(x => x.Email == email);
        if (pending is not null && DateTime.UtcNow - pending.LastSentUtc < TimeSpan.FromSeconds(60))
            return StatusCode(429, "Wait one minute before requesting another code.");
        if (await db.PendingSignups.AnyAsync(x => x.Email != email && x.Mobile.Replace("+", "").Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace(".", "") == mobileDigits))
            return Conflict("This mobile number is already being used for another signup.");

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        try
        {
            await SendConfiguredEmailAsync(email, "Verify your Ubhaya Fashions email",
                $"Your verification code is {code}. It expires in 10 minutes. If you did not request this, you can ignore this email.");
        }
        catch (InvalidOperationException ex) { return StatusCode(503, ex.Message); }
        catch (SmtpException) { return StatusCode(503, "Could not send the verification email. Check the backend SMTP settings."); }

        if (pending is null)
        {
            pending = new PendingSignup { Email = email };
            db.PendingSignups.Add(pending);
        }
        pending.Name = request.Name.Trim();
        pending.Mobile = mobile;
        pending.Age = request.Age;
        pending.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        pending.CodeHash = HashSignupCode(email, code);
        pending.ExpiresUtc = DateTime.UtcNow.AddMinutes(10);
        pending.LastSentUtc = DateTime.UtcNow;
        pending.FailedAttempts = 0;
        await db.SaveChangesAsync();
        return Ok(new { success = true, message = "A verification code has been sent to your email." });
    }

    [HttpPost("signup/resend-otp")]
    public async Task<IActionResult> ResendSignupOtp(ResendSignupOtpRequest request)
    {
        var email = request.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email)) return BadRequest("Email is required.");
        var pending = await db.PendingSignups.FirstOrDefaultAsync(x => x.Email == email);
        if (pending is null) return Ok(new { success = true });
        if (DateTime.UtcNow - pending.LastSentUtc < TimeSpan.FromSeconds(60))
            return StatusCode(429, "Wait one minute before requesting another code.");
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        try
        {
            await SendConfiguredEmailAsync(email, "Your Ubhaya Fashions verification code",
                $"Your new verification code is {code}. It expires in 10 minutes.");
        }
        catch (InvalidOperationException ex) { return StatusCode(503, ex.Message); }
        catch (SmtpException) { return StatusCode(503, "Could not send the verification email. Check the backend SMTP settings."); }
        pending.CodeHash = HashSignupCode(email, code);
        pending.ExpiresUtc = DateTime.UtcNow.AddMinutes(10);
        pending.LastSentUtc = DateTime.UtcNow;
        pending.FailedAttempts = 0;
        await db.SaveChangesAsync();
        return Ok(new { success = true, message = "A new verification code has been sent." });
    }

    [HttpPost("signup/verify-email")]
    public async Task<IActionResult> VerifySignupEmail(VerifySignupEmailRequest request)
    {
        var email = request.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || !Regex.IsMatch(request.Code ?? "", "^[0-9]{6}$"))
            return BadRequest("Enter the email address and its 6-digit verification code.");
        var pending = await db.PendingSignups.FirstOrDefaultAsync(x => x.Email == email);
        if (pending is null || pending.ExpiresUtc <= DateTime.UtcNow)
            return BadRequest("That code is invalid or expired. Request a new code.");
        if (pending.FailedAttempts >= 5)
            return StatusCode(429, "Too many incorrect attempts. Request a new code.");
        var expectedHash = Convert.FromHexString(pending.CodeHash);
        var suppliedHash = Convert.FromHexString(HashSignupCode(email, request.Code ?? ""));
        if (!CryptographicOperations.FixedTimeEquals(expectedHash, suppliedHash))
        {
            pending.FailedAttempts++;
            await db.SaveChangesAsync();
            return BadRequest("The verification code is incorrect.");
        }
        if (await db.Customers.AnyAsync(x => x.Email == email))
            return Conflict("An account with this email already exists. Please login.");
        var pendingMobileDigits = NormalizePhone(pending.Mobile);
        if (await db.Customers.AnyAsync(x => x.Mobile != null &&
            x.Mobile.Replace("+", "").Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace(".", "") == pendingMobileDigits))
            return Conflict("An account with this mobile number already exists. Please login.");

        await using var transaction = await db.Database.BeginTransactionAsync();
        db.Customers.Add(new Customer
        {
            Name = pending.Name,
            Email = pending.Email,
            Mobile = pending.Mobile,
            Age = pending.Age,
            PasswordHash = pending.PasswordHash
        });
        db.PendingSignups.Remove(pending);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new { success = true, message = "Email verified and account created." });
    }



    // =====================================================
    // LOGIN
    // =====================================================

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Identifier))
        {
            return BadRequest(
                "Email or mobile number is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(
                "Password is required.");
        }

        var identifier = request.Identifier.Trim();
        Customer? customer;
        if (identifier.Contains('@'))
        {
            var email = identifier.ToLowerInvariant();
            customer = await db.Customers.FirstOrDefaultAsync(x => x.Email == email);
        }
        else
        {
            var mobileDigits = NormalizePhone(identifier);
            customer = await db.Customers.FirstOrDefaultAsync(x => x.Mobile != null &&
                x.Mobile.Replace("+", "").Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace(".", "") == mobileDigits);
        }

        if (customer is null)
        {
            return Unauthorized(
                "Invalid email or password.");
        }

        if (string.IsNullOrWhiteSpace(
                customer.PasswordHash))
        {
            return Unauthorized(
                "Invalid email or password.");
        }

        var validPassword =
            BCrypt.Net.BCrypt.Verify(
                request.Password,
                customer.PasswordHash);

        if (!validPassword)
        {
            return Unauthorized(
                "Invalid email or password.");
        }

        return Ok(
            CreateLoginResponse(customer));
    }


    // =====================================================
    // GET MY PROFILE
    // =====================================================

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var customerIdText =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!int.TryParse(
                customerIdText,
                out var customerId))
        {
            return Unauthorized(
                "Invalid customer authentication.");
        }

        var customer =
            await db.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == customerId);

        if (customer is null)
        {
            return NotFound(
                "Customer not found.");
        }

        return Ok(
            new ProfileResponse(
                customer.Id,
                customer.Name,
                customer.Email,
                customer.Mobile,
                customer.Age,
                customer.ProfileImage,
                customer.DateOfBirth,
                customer.Gender));
    }


    // =====================================================
    // UPDATE MY PROFILE
    // =====================================================

    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile(
        UpdateProfileRequest request)
    {
        var customerIdText =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!int.TryParse(
                customerIdText,
                out var customerId))
        {
            return Unauthorized(
                "Invalid customer authentication.");
        }

        var customer =
            await db.Customers
                .FirstOrDefaultAsync(
                    x => x.Id == customerId);

        if (customer is null)
        {
            return NotFound(
                "Customer not found.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(
                "Name is required.");
        }

        if (request.Age.HasValue &&
            (request.Age < 18 ||
             request.Age > 120))
        {
            return BadRequest(
                "Age must be between 18 and 120.");
        }

        customer.Name =
            request.Name.Trim();

        customer.Mobile =
            string.IsNullOrWhiteSpace(request.Mobile)
                ? null
                : request.Mobile.Trim();

        if (customer.Mobile is not null)
        {
            var mobileDigits = NormalizePhone(customer.Mobile);
            if (mobileDigits.Length is < 7 or > 15)
                return BadRequest("Enter a valid mobile number.");
            var duplicateMobile = await db.Customers.AnyAsync(x => x.Id != customerId && x.Mobile != null &&
                x.Mobile.Replace("+", "").Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace(".", "") == mobileDigits);
            if (duplicateMobile)
                return Conflict("This mobile number is already used by another account.");
        }

        customer.Age =
            request.Age;

        customer.DateOfBirth = request.DateOfBirth;
        customer.Gender = string.IsNullOrWhiteSpace(request.Gender) ? null : request.Gender.Trim();

        await db.SaveChangesAsync();

        return Ok(
            new ProfileResponse(
                customer.Id,
                customer.Name,
                customer.Email,
                customer.Mobile,
                customer.Age,
                customer.ProfileImage,
                customer.DateOfBirth,
                customer.Gender));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        if (!TryGetCustomerId(out var customerId)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            return BadRequest("New password must contain at least 8 characters.");

        var customer = await db.Customers.FirstOrDefaultAsync(x => x.Id == customerId);
        if (customer is null || string.IsNullOrWhiteSpace(customer.PasswordHash) ||
            !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, customer.PasswordHash))
            return BadRequest("Current password is incorrect.");

        customer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        customer.PasswordResetTokenHash = null;
        customer.PasswordResetExpiresUtc = null;
        await db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        var smtp = config.GetSection("Smtp");
        var host = smtp["Host"];
        var from = smtp["From"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
            return StatusCode(503, "Password reset email is not configured. Set the Smtp configuration for the backend.");

        var email = request.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email)) return Ok(new { success = true });
        var customer = await db.Customers.FirstOrDefaultAsync(x => x.Email == email);
        if (customer is null) return Ok(new { success = true });

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        customer.PasswordResetTokenHash = HashResetToken(token);
        customer.PasswordResetExpiresUtc = DateTime.UtcNow.AddHours(1);
        await db.SaveChangesAsync();

        var resetUrl = $"{GetFrontendUrl()}/Account/ResetPassword?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
        try
        {
            using var message = new MailMessage(from, email)
            {
                Subject = "Reset your Ubhaya Fashions password",
                Body = $"Use this one-time link to reset your password. It expires in one hour:\n\n{resetUrl}\n\nIf you did not request this, you can ignore this email.",
                IsBodyHtml = false
            };
            var port = int.TryParse(smtp["Port"], out var configuredPort) ? configuredPort : 587;
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = bool.TryParse(smtp["EnableSsl"], out var ssl) ? ssl : true,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };
            var username = smtp["Username"];
            var password = smtp["Password"];
            if (!string.IsNullOrWhiteSpace(username))
                client.Credentials = new NetworkCredential(username, password);
            await client.SendMailAsync(message);
        }
        catch (Exception)
        {
            customer.PasswordResetTokenHash = null;
            customer.PasswordResetExpiresUtc = null;
            await db.SaveChangesAsync();
            return StatusCode(503, "Could not send the reset email. Check the backend SMTP settings.");
        }

        return Ok(new { success = true });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            return BadRequest("Password must contain at least 8 characters.");
        var email = request.Email?.Trim().ToLowerInvariant();
        var customer = await db.Customers.FirstOrDefaultAsync(x => x.Email == email);
        var tokenHash = HashResetToken(request.Token ?? "");
        if (customer is null || customer.PasswordResetTokenHash is null ||
            customer.PasswordResetExpiresUtc <= DateTime.UtcNow ||
            !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(customer.PasswordResetTokenHash),
                Encoding.UTF8.GetBytes(tokenHash)))
            return BadRequest("This reset link is invalid or has expired. Request a new one.");

        customer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        customer.PasswordResetTokenHash = null;
        customer.PasswordResetExpiresUtc = null;
        await db.SaveChangesAsync();
        return Ok(new { success = true });
    }


    // =====================================================
    // GOOGLE
    // =====================================================

    [HttpGet("google")]
    public IActionResult Google()
    {
        return Challenge(
            new AuthenticationProperties
            {
                RedirectUri =
                    "/api/auth/google-response"
            },
            "Google");
    }


    // =====================================================
    // FACEBOOK
    // =====================================================

    [HttpGet("facebook")]
    public IActionResult Facebook()
    {
        return Challenge(
            new AuthenticationProperties
            {
                RedirectUri =
                    "/api/auth/facebook-response"
            },
            "Facebook");
    }


    // =====================================================
    // GOOGLE RESPONSE
    // =====================================================

    [HttpGet("google-response")]
    public Task<IActionResult> GoogleResponse()
    {
        return HandleExternalResponse("Google");
    }


    // =====================================================
    // FACEBOOK RESPONSE
    // =====================================================

    [HttpGet("facebook-response")]
    public Task<IActionResult> FacebookResponse()
    {
        return HandleExternalResponse("Facebook");
    }


    // =====================================================
    // EXTERNAL LOGIN
    // =====================================================

    private async Task<IActionResult>
        HandleExternalResponse(string provider)
    {
        var result =
            await HttpContext.AuthenticateAsync(
                CookieAuthenticationDefaults
                    .AuthenticationScheme);

        if (!result.Succeeded ||
            result.Principal is null)
        {
            return Redirect(
                GetFrontendUrl() +
                "/login?error=authentication_failed");
        }

        var principal =
            result.Principal;

        var providerId =
            principal.FindFirstValue(
                ClaimTypes.NameIdentifier)
            ??
            principal.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(
                providerId))
        {
            return Redirect(
                GetFrontendUrl() +
                "/login?error=missing_provider_id");
        }

        var email =
            principal.FindFirstValue(
                ClaimTypes.Email);

        var name =
            principal.FindFirstValue(
                ClaimTypes.Name)
            ??
            principal.FindFirstValue("name")
            ??
            "Customer";

        var image =
            principal.FindFirstValue("picture");

        var external =
            await db.ExternalLogins
                .Include(x => x.Customer)
                .FirstOrDefaultAsync(
                    x =>
                        x.Provider == provider &&
                        x.ProviderUserId == providerId);

        Customer customer;

        if (external is not null)
        {
            customer =
                external.Customer;
        }
        else
        {
            customer =
                email is not null
                    ? await db.Customers
                        .FirstOrDefaultAsync(
                            x => x.Email == email)
                      ?? new Customer()
                    : new Customer();

            if (customer.Id == 0)
            {
                customer.Name = name;

                customer.Email = email;

                customer.ProfileImage = image;

                db.Customers.Add(customer);

                await db.SaveChangesAsync();
            }

            db.ExternalLogins.Add(
                new ExternalLogin
                {
                    CustomerId = customer.Id,

                    Provider = provider,

                    ProviderUserId = providerId,

                    Email = email
                });

            await db.SaveChangesAsync();
        }

        var token =
            CreateJwt(customer);

        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults
                .AuthenticationScheme);

        return Redirect(
            $"{GetFrontendUrl()}/oauth-callback?token={Uri.EscapeDataString(token)}");
    }


    private string HashSignupCode(string email, string code)
    {
        var key = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing.");
        return Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(email + ":" + code)));
    }

    private async Task SendConfiguredEmailAsync(string recipient, string subject, string body)
    {
        var smtp = config.GetSection("Smtp");
        var host = smtp["Host"];
        var from = smtp["From"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("Email verification is not configured. Set the backend Smtp configuration.");
        using var message = new MailMessage(from, recipient) { Subject = subject, Body = body, IsBodyHtml = false };
        var port = int.TryParse(smtp["Port"], out var configuredPort) ? configuredPort : 587;
        using var client = new SmtpClient(host, port)
        {
            EnableSsl = bool.TryParse(smtp["EnableSsl"], out var ssl) ? ssl : true,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };
        var username = smtp["Username"];
        if (!string.IsNullOrWhiteSpace(username)) client.Credentials = new NetworkCredential(username, smtp["Password"]);
        await client.SendMailAsync(message);
    }
    // =====================================================
    // CREATE LOGIN RESPONSE
    // =====================================================

    private bool TryGetCustomerId(out int customerId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out customerId);

    private static string NormalizePhone(string value) =>
        Regex.Replace(value, @"\D", "");

    private static string HashResetToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private LoginResponse CreateLoginResponse(
        Customer customer)
    {
        return new LoginResponse(
            CreateJwt(customer),
            customer.Id,
            customer.Name,
            customer.Email,
            customer.Age,
            customer.ProfileImage);
    }


    // =====================================================
    // CREATE JWT
    // =====================================================

    private string CreateJwt(Customer customer)
    {
        var keyText =
            config["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "Jwt:Key is missing.");

        var issuer =
            config["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "Jwt:Issuer is missing.");

        var audience =
            config["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "Jwt:Audience is missing.");

        var minutes =
            int.TryParse(
                config["Jwt:Minutes"],
                out var m)
                ? m
                : 120;

        var key =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(keyText));

        var claims = new[]
        {
        new Claim(
            ClaimTypes.NameIdentifier,
            customer.Id.ToString()),

        new Claim(
            ClaimTypes.Name,
            customer.Name),

        new Claim(
            ClaimTypes.Email,
            customer.Email ?? "")
    };

        var token =
            new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(minutes),
                signingCredentials:
                    new SigningCredentials(
                        key,
                        SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    // =====================================================
    // FRONTEND URL
    // =====================================================

    private string GetFrontendUrl()
    {
        return
            config["FrontendUrl"]?
                .TrimEnd('/')
            ??
            "http://localhost:5001";
    }
}



