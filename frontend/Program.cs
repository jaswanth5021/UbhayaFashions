using LadiesDressStore.Web;
using LadiesDressStore.Web.Middleware;
using LadiesDressStore.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.FileProviders;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// MVC
// =====================================================

builder.Services.AddControllersWithViews(options => options.Filters.AddService<UserErrorLogFilter>());
builder.Services.AddMemoryCache();

// =====================================================
// HTTP CONTEXT
// =====================================================

builder.Services.AddHttpContextAccessor();

// =====================================================
// API HTTP CLIENT
// =====================================================

var apiBaseUrl =
    builder.Configuration["ApiBaseUrl"]
    ?? "http://localhost:5000/";

builder.Services
    .AddHttpClient(
        "Api",
        client =>
        {
            client.BaseAddress =
                new Uri(apiBaseUrl);

            client.Timeout =
                TimeSpan.FromSeconds(60);
        })
    .ConfigurePrimaryHttpMessageHandler(
        () =>
        {
            var handler = new HttpClientHandler();

            if (builder.Environment.IsDevelopment())
            {
                // Accept the local ASP.NET development certificate
                // only in development.
                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler
                        .DangerousAcceptAnyServerCertificateValidator;
            }

            return handler;
        });

// =====================================================
// API SERVICE
// =====================================================

builder.Services.AddScoped<ApiService>();
builder.Services.AddScoped<AdminApiService>();
builder.Services.AddSingleton<IErrorLog, ErrorLog>();
builder.Services.AddScoped<UserErrorLogFilter>();

// =====================================================
// FRONTEND COOKIE AUTHENTICATION - SECURITY FIX
// =====================================================
// Cookies now use Secure flag in production (HTTPS only)
// This prevents session theft over unencrypted connections
// =====================================================

builder.Services
    .AddAuthentication(options =>
    {
        // Store pages use the customer cookie. Admin pages use a separate cookie.
        options.DefaultScheme = AuthSchemes.Frontend;
        options.DefaultAuthenticateScheme = AuthSchemes.Frontend;
        options.DefaultChallengeScheme = AuthSchemes.Frontend;
    })
    .AddPolicyScheme(AuthSchemes.Frontend, AuthSchemes.Frontend, options =>
    {
        options.ForwardDefaultSelector = context =>
            context.Request.Path.StartsWithSegments("/Admin")
                ? AuthSchemes.Admin
                : CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(
        CookieAuthenticationDefaults
            .AuthenticationScheme,
        options =>
        {
            options.Cookie.Name =
                "UbhayaAuth";

            options.LoginPath =
                "/Account/Login";

            options.AccessDeniedPath =
                "/Account/Login";

            options.Cookie.HttpOnly =
                true;

            options.Cookie.SameSite =
                SameSiteMode.Lax;

            // SECURITY FIX: Use Secure cookies in production (HTTPS only)
            options.Cookie.SecurePolicy =
                builder.Environment.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;

            options.ExpireTimeSpan =
                TimeSpan.FromHours(2);

            options.SlidingExpiration =
                true;
        })
    .AddCookie(AuthSchemes.Admin, options =>
    {
        options.Cookie.Name = "UbhayaAdminAuth";
        options.LoginPath = "/Admin/Login";
        options.AccessDeniedPath = "/Admin/Login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        
        // SECURITY FIX: Use Secure cookies in production (HTTPS only)
        options.Cookie.SecurePolicy =
            builder.Environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
                
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;
    });

// =====================================================
// AUTHORIZATION
// =====================================================

builder.Services.AddAuthorization();

// =====================================================
// BUILD
// =====================================================

var app = builder.Build();

// =====================================================
// PIPELINE
// =====================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
        "/Home/Error");
}

app.UseStatusCodePagesWithReExecute("/Home/NotFoundPage");

// =====================================================
// HTTPS - SECURITY FIX
// =====================================================
// HTTPS redirection now enabled for production
// HSTS header added for browsers to enforce HTTPS
// =====================================================

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// =====================================================
// STATIC FILES
// =====================================================

app.UseStaticFiles();

// Expose uploaded media from the shared upload directory under /uploads.
// Configure UploadsPath to the same directory used by the API, especially on EC2.
var uploadsPath = builder.Configuration["UploadsPath"];
if (string.IsNullOrWhiteSpace(uploadsPath))
{
    uploadsPath = OperatingSystem.IsWindows()
        ? @"C:\UbhayaFashions\Uploads"
        : Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "uploads"));
}
var fullUploadsPath = Path.GetFullPath(uploadsPath);
Directory.CreateDirectory(fullUploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(fullUploadsPath),
    RequestPath = "/uploads"
});

// =====================================================
// ROUTING
// =====================================================

app.UseRouting();

// =====================================================
// AUTHENTICATION
// =====================================================

app.UseAuthentication();
app.UseMiddleware<ErrorLogMiddleware>();

// An older admin login was stored in the customer cookie. Drop that session
// on the store so the shopper is asked to sign in with a customer account.
app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/Admin")
        && context.User.HasClaim("AdminSession", "true"))
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        context.User = new ClaimsPrincipal(new ClaimsIdentity());
    }

    await next();
});

app.UseAuthorization();

// =====================================================
// ROUTES
// =====================================================

app.MapControllerRoute(
    name: "default",
    pattern:
        "{controller=Home}/{action=Index}/{id?}");

// =====================================================
// RUN
// =====================================================

app.Run();
