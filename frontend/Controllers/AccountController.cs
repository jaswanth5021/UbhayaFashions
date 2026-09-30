using LadiesDressStore.Web.Models;
using LadiesDressStore.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LadiesDressStore.Web.Controllers;

public class AccountController(ApiService api) : Controller
{
    // =====================================================
    // LOGIN - GET
    // =====================================================

    [HttpGet]
    public IActionResult Login(string? error = null, string? returnUrl = null)
    {
        ViewBag.Error = error;
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }


    // =====================================================
    // LOGIN - POST
    // =====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var result = await api.LoginAsync(model);

        if (!result.Success || result.Data == null)
        {
            ModelState.AddModelError(
                "",
                result.Message.Trim('"'));

            ViewBag.ReturnUrl = returnUrl;

            return View(model);
        }

        // Create frontend authentication cookie
        await SignInUser(result.Data);

        TempData["LoginSuccess"] =
            $"Welcome back, {result.Data.Name}!";

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(
            "Index",
            "Home");
    }


    // =====================================================
    // SIGNUP - GET
    // =====================================================

    [HttpGet]
    public IActionResult Signup()
    {
        return View();
    }


    // =====================================================
    // SIGNUP - POST
    // =====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Signup(SignupViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await api.SignupAsync(model);

        if (!result.Success)
        {
            ModelState.AddModelError(
                "",
                result.Message.Trim('"'));

            return View(model);
        }

        TempData["SignupCodeSent"] = "We sent a verification code to your email.";
        return RedirectToAction(nameof(VerifySignupEmail), new { email = model.Email });
    }

    [HttpGet]
    public IActionResult VerifySignupEmail(string? email) =>
        View(new VerifySignupEmailViewModel { Email = email ?? "" });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifySignupEmail(VerifySignupEmailViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await api.VerifySignupEmailAsync(model.Email, model.Code);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message.Trim('"'));
            return View(model);
        }
        TempData["SignupSuccess"] = "Email verified and account created. Please log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendSignupCode(string email)
    {
        var result = await api.ResendSignupCodeAsync(email);
        TempData[result.Success ? "SignupCodeSent" : "SignupCodeError"] = result.Success
            ? "A new verification code has been sent."
            : result.Message.Trim('"');
        return RedirectToAction(nameof(VerifySignupEmail), new { email });
    }


    // =====================================================
    // PROFILE - GET
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Profile(string tab = "details")
    {
        // IMPORTANT:
        // Do not redirect immediately.
        // Let's see whether the frontend cookie exists.

        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction(nameof(Login));
        }

        var result =
            await api.GetMyProfileAsync();

        if (!result.Success)
        {
            ViewBag.Error =
                $"Profile API returned {result.StatusCode}: {result.Message}";

            return View(new ProfileViewModel());
        }

        ViewBag.ActiveTab = tab;
        try
        {
            ViewBag.SavedAddresses = await api.GetMyAddressesAsync();
        }
        catch (HttpRequestException ex)
        {
            ViewBag.SavedAddresses = new List<SavedAddressViewModel>();
            ViewBag.AddressLoadError = ex.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden
                ? "Your session may have expired. Please sign out and sign in again."
                : $"The address service could not load your saved addresses ({(int?)ex.StatusCode ?? 0}). Please try again later.";
        }
        return View(result.Data);
    }


    // =====================================================
    // PROFILE - POST
    // =====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(
        ProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ProfileError"] = "Please check the profile fields and try again.";
            return RedirectToAction(nameof(Profile), new { tab = "details" });
        }

        if (User.Identity?.IsAuthenticated != true)
            return RedirectToAction(nameof(Login));

        var result =
            await api.UpdateMyProfileAsync(model);

        if (!result.Success)
        {
            ViewBag.Error =
                $"Profile update failed: {result.Message}";

            return View(model);
        }

        TempData["ProfileSuccess"] =
            "Profile updated successfully.";

        return RedirectToAction(nameof(Profile), new { tab = "details" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAddress(SaveAddressViewModel model)
    {
        if (User.Identity?.IsAuthenticated != true)
            return RedirectToAction(nameof(Login));

        var result = await api.SaveAddressAsync(model);
        TempData[result.Success ? "ProfileSuccess" : "ProfileError"] = result.Success
            ? "Address saved successfully."
            : result.Message.Trim('"');
        return RedirectToAction(nameof(Profile), new { tab = "addresses" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAddress(int id)
    {
        if (User.Identity?.IsAuthenticated != true)
            return RedirectToAction(nameof(Login));

        var result = await api.DeleteAddressAsync(id);
        TempData[result.Success ? "ProfileSuccess" : "ProfileError"] = result.Success
            ? "Address removed."
            : result.Message.Trim('"');
        return RedirectToAction(nameof(Profile), new { tab = "addresses" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (User.Identity?.IsAuthenticated != true)
            return RedirectToAction(nameof(Login));

        if (!ModelState.IsValid)
        {
            TempData["ProfileError"] = "Check the password fields and try again.";
            return RedirectToAction(nameof(Profile), new { tab = "security" });
        }

        var result = await api.ChangePasswordAsync(model);
        TempData[result.Success ? "ProfileSuccess" : "ProfileError"] = result.Success
            ? "Password updated successfully."
            : result.Message.Trim('"');
        return RedirectToAction(nameof(Profile), new { tab = "security" });
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await api.RequestPasswordResetAsync(model.Email);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message.Trim('"'));
            return View(model);
        }

        TempData["PasswordResetSent"] = "If an account exists for that email, a password reset link has been sent.";
        return RedirectToAction(nameof(ForgotPassword));
    }

    [HttpGet]
    public IActionResult ResetPassword(string? email, string? token) =>
        View(new ResetPasswordViewModel { Email = email ?? "", Token = token ?? "" });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await api.ResetPasswordAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message.Trim('"'));
            return View(model);
        }

        TempData["SuccessMessage"] = "Password reset. You can now log in.";
        return RedirectToAction(nameof(Login));
    }


    // =====================================================
    // LOGOUT
    // =====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        // Remove JWT cookie
        Response.Cookies.Delete("access_token");

        // Remove frontend authentication cookie
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction(
            "Index",
            "Home");
    }


    // =====================================================
    // CREATE FRONTEND LOGIN SESSION
    // =====================================================

    
  private async Task SignInUser(AuthResponse auth)
{
    // =====================================================
    // 1. Store JWT for Backend API calls
    // =====================================================

    Response.Cookies.Append(
        "access_token",
        auth.Token,
        new CookieOptions
        {
            HttpOnly = true,

            // Frontend is running on HTTP localhost:5001
            Secure = Request.IsHttps,

            SameSite = SameSiteMode.Lax,

            Expires =
                DateTimeOffset.UtcNow.AddHours(2)
        });


    // =====================================================
    // 2. Create Frontend Authentication Session
    // =====================================================

        var claims = new List<Claim>
        {
        new Claim(
            ClaimTypes.NameIdentifier,
            auth.CustomerId.ToString()),

        new Claim(
                ClaimTypes.Name,
                auth.Name)
        };

        if (!string.IsNullOrWhiteSpace(auth.Token))
        {
            claims.Add(new Claim("ApiAccessToken", auth.Token));
        }

    if (!string.IsNullOrWhiteSpace(auth.Email))
    {
        claims.Add(
            new Claim(
                ClaimTypes.Email,
                auth.Email));
    }

    var identity =
        new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

    var principal =
        new ClaimsPrincipal(identity);


    // Remove old frontend session
    await HttpContext.SignOutAsync(
        CookieAuthenticationDefaults.AuthenticationScheme);


    // Create new frontend session
    await HttpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal,
        new AuthenticationProperties
        {
            IsPersistent = true,
            AllowRefresh = true,

            ExpiresUtc =
                DateTimeOffset.UtcNow.AddHours(2)
        });
}
}
