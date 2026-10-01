using LadiesDressStore.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// MVC
// =====================================================

builder.Services.AddControllersWithViews();
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

// =====================================================
// FRONTEND COOKIE AUTHENTICATION
// =====================================================

builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults
            .AuthenticationScheme)
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
                CookieSecurePolicy.SameAsRequest;

            options.ExpireTimeSpan =
                TimeSpan.FromHours(2);

            options.SlidingExpiration =
                true;
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

app.UseStaticFiles();

// =====================================================
// ROUTING
// =====================================================

app.UseRouting();

// =====================================================
// AUTHENTICATION
// =====================================================

app.UseAuthentication();

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