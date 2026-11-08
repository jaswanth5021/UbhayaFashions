using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using LadiesDressStore.Web;
using LadiesDressStore.Web.Middleware;
using LadiesDressStore.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.FileProviders;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    foreach (var proxy in builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [])
        options.KnownProxies.Add(System.Net.IPAddress.Parse(proxy));
});
var keyPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(keyPath))
    builder.Services.AddDataProtection().SetApplicationName("UbhayaStorefront")
        .PersistKeysToFileSystem(new DirectoryInfo(keyPath));
builder.Services.AddHealthChecks();

// =====================================================
// MVC
// =====================================================

builder.Services.AddControllersWithViews(options => options.Filters.AddService<UserErrorLogFilter>());
builder.Services.AddMemoryCache();
builder.Services.AddResponseCompression(options =>
{
    // Compression middleware is placed only in the static asset path branch
    // below, so enabling it for HTTPS does not compress personalized HTML.
    options.EnableForHttps = true;
});

// =====================================================
// HTTP CONTEXT
// =====================================================

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ApiForwardedHeadersHandler>();

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
    .AddHttpMessageHandler<ApiForwardedHeadersHandler>()
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
// FRONTEND COOKIE AUTHENTICATION
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

            // Required for the current HTTP-only EC2 test setup.
            // When HTTPS is configured later, change this to Always.
            options.Cookie.SecurePolicy =
                builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;

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
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
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
app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// =====================================================
// PIPELINE
// =====================================================

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(self), camera=(), microphone=()";
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
        "/Home/Error");
}

app.UseStatusCodePagesWithReExecute("/Home/NotFoundPage");

// =====================================================
// HTTPS
// =====================================================
// HTTPS is not configured yet.
// Keep HTTP for the current EC2 test environment.
//
// When HTTPS is configured later, enable:
// app.UseHsts();
// app.UseHttpsRedirection();

// =====================================================
// STATIC FILES
// =====================================================

// Compress static assets only; personalized storefront HTML can contain user data.
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/css")
        || context.Request.Path.StartsWithSegments("/js")
        || context.Request.Path.StartsWithSegments("/images")
        || context.Request.Path.StartsWithSegments("/uploads"),
    branch => branch.UseResponseCompression());

const string longLivedAssetCacheControl = "public,max-age=31536000,immutable";
const string shortLivedAssetCacheControl = "public,max-age=86400";

// Versioned static assets can stay cached for a year because their URL changes
// when their contents change.
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        context.Context.Response.Headers.CacheControl =
            context.Context.Request.Query.ContainsKey("v")
                ? longLivedAssetCacheControl
                : shortLivedAssetCacheControl;
    }
});

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
    RequestPath = "/uploads",
    OnPrepareResponse = context =>
    {
        // Product uploads use unique GUID filenames, so replacing an image creates
        // a new URL and these files can be cached immutably. Keep legacy names short-lived.
        var fileStem = Path.GetFileNameWithoutExtension(context.Context.Request.Path.Value ?? string.Empty);
        var isUniqueUpload = Guid.TryParseExact(fileStem, "N", out _)
            || (fileStem.StartsWith("banner-", StringComparison.OrdinalIgnoreCase)
                && Guid.TryParseExact(fileStem["banner-".Length..], "N", out _));
        context.Context.Response.Headers.CacheControl = isUniqueUpload
            ? longLivedAssetCacheControl
            : shortLivedAssetCacheControl;
    }
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

app.MapHealthChecks("/health/live");
app.Run();
