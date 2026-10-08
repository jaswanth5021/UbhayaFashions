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

    [HttpPost("signup")]
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

        db.Customers.Add(new Customer
        {
            Name = request.Name.Trim(),
            Email = email,
            Mobile = mobile,
            Age = request.Age,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            EmailVerified = false
        });
        await db.SaveChangesAsync();
        return Ok(new { success = true, message = "Account created. You can verify your email from your profile after logging in." });
    }

    [Authorize]
    [HttpPost("me/email-verification/send")]
    public async Task<IActionResult> SendMyEmailVerification()
    {
        if (!TryGetCustomerId(out var customerId)) return Unauthorized();
        var customer = await db.Customers.FirstOrDefaultAsync(x => x.Id == customerId);
        if (customer is null) return NotFound("Customer not found.");
        if (customer.EmailVerified) return BadRequest("Your email is already verified.");
        if (string.IsNullOrWhiteSpace(customer.Email)) return BadRequest("Add an email address to your account first.");
        if (customer.EmailVerificationLastSentUtc is DateTime lastSent && DateTime.UtcNow - lastSent < TimeSpan.FromSeconds(60))
            return StatusCode(429, "Wait one minute before requesting another code.");

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        try
        {
            await SendConfiguredEmailAsync(customer.Email, "Verify your Ubhaya Fashions email", code);
        }
        catch (InvalidOperationException ex) { return StatusCode(503, ex.Message); }
        catch (SmtpException) { return StatusCode(503, "Could not send the verification email. Check the backend SMTP settings."); }

        customer.EmailVerificationCodeHash = HashSignupCode(customer.Email, code);
        customer.EmailVerificationCodeExpiresUtc = DateTime.UtcNow.AddMinutes(10);
        customer.EmailVerificationLastSentUtc = DateTime.UtcNow;
        customer.EmailVerificationFailedAttempts = 0;
        await db.SaveChangesAsync();
        return Ok(new { success = true, message = "Verification code sent." });
    }

    [Authorize]
    [HttpPost("me/email-verification/confirm")]
    public async Task<IActionResult> ConfirmMyEmailVerification(VerifyMyEmailRequest request)
    {
        if (!TryGetCustomerId(out var customerId)) return Unauthorized();
        var customer = await db.Customers.FirstOrDefaultAsync(x => x.Id == customerId);
        if (customer is null) return NotFound("Customer not found.");
        if (customer.EmailVerified) return Ok(new { success = true });
        if (string.IsNullOrWhiteSpace(customer.Email) || !Regex.IsMatch(request.Code ?? "", "^[0-9]{6}$"))
            return BadRequest("Enter the 6-digit verification code from your email.");
        if (string.IsNullOrWhiteSpace(customer.EmailVerificationCodeHash) || customer.EmailVerificationCodeExpiresUtc <= DateTime.UtcNow)
            return BadRequest("That code is invalid or expired. Request a new code.");
        if (customer.EmailVerificationFailedAttempts >= 5)
            return StatusCode(429, "Too many incorrect attempts. Request a new code.");

        var expectedHash = Convert.FromHexString(customer.EmailVerificationCodeHash);
        var suppliedHash = Convert.FromHexString(HashSignupCode(customer.Email, request.Code));
        if (!CryptographicOperations.FixedTimeEquals(expectedHash, suppliedHash))
        {
            customer.EmailVerificationFailedAttempts++;
            await db.SaveChangesAsync();
            return BadRequest("The verification code is incorrect.");
        }

        customer.EmailVerified = true;
        customer.EmailVerificationCodeHash = null;
        customer.EmailVerificationCodeExpiresUtc = null;
        customer.EmailVerificationFailedAttempts = 0;
        await db.SaveChangesAsync();
        return Ok(new { success = true, message = "Email verified successfully." });
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
            await SendConfiguredEmailAsync(email, "Your Ubhaya Fashions verification code", code);
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
                customer.Gender,
                customer.EmailVerified));
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
                customer.Gender,
                customer.EmailVerified));
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

    private async Task SendConfiguredEmailAsync(string recipient, string subject, string code)
    {
        try
        {

       
        var smtp = config.GetSection("Smtp");
        var host = smtp["Host"];
        var from = smtp["From"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("Email verification is not configured. Set the backend Smtp configuration.");
        var plainText = $"Your Ubhaya Fashions verification code is {code}. It expires in 10 minutes. If you did not request this, you can ignore this email.";
        var html = $"""
                <!doctype html>
                <html lang="en">
                <body style="margin:0;padding:0;background-color:#f6f3ef;font-family:Arial,Helvetica,sans-serif;color:#292421;">
                  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background-color:#f6f3ef;padding:36px 12px;">
                    <tr><td align="center">
                      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:560px;background:#ffffff;border-radius:14px;overflow:hidden;">
                        <tr><td style="background:#542b35;padding:26px 32px;text-align:center;color:#ffffff;">
                          <div style="font-size:12px;letter-spacing:3px;text-transform:uppercase;color:#f1d8b5;">Ubhaya Fashions</div>
                          <div style="font-size:23px;font-weight:600;margin-top:10px;">Verify your email</div>
                        </td></tr>
                        <tr><td style="padding:32px;text-align:center;">
                          <p style="font-size:16px;line-height:1.6;margin:0 0 22px;">Enter this verification code to continue creating your account:</p>
                          <div style="display:inline-block;background:#f8f4ef;border:1px solid #eadfD2;border-radius:10px;padding:15px 26px;color:#542b35;font-size:32px;font-weight:700;letter-spacing:8px;">{code}</div>
                          <p style="font-size:14px;line-height:1.6;color:#746b65;margin:22px 0 0;">This code expires in <strong>10 minutes</strong>.</p>
                          <p style="font-size:13px;line-height:1.6;color:#8b817a;margin:18px 0 0;">If you did not request this email, you can safely ignore it.</p>
                        </td></tr>
                        <tr><td style="border-top:1px solid #eee8e2;padding:18px 28px;text-align:center;color:#9a918a;font-size:12px;">A little elegance, made for you.</td></tr>
                      </table>
                    </td></tr>
                  </table>
                </body>
                </html>
                """;
        using var message = new MailMessage(from, recipient) { Subject = subject, Body = plainText, IsBodyHtml = false };
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(plainText, null, "text/plain"));
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, null, "text/html"));
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
        catch (Exception ex)
        {

            throw;
        }
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



