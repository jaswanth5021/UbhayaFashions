using backend.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// CONTROLLERS
// =====================================================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Razorpay REST client. The secret is supplied only by backend configuration.
builder.Services.AddHttpClient("Razorpay", client =>
{
    client.BaseAddress = new Uri("https://api.razorpay.com/v1/");
    client.Timeout = TimeSpan.FromSeconds(30);
});


// =====================================================
// DATABASE
// =====================================================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "DefaultConnection")));


// =====================================================
// JWT CONFIGURATION
// =====================================================

var jwtKey =
    builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Jwt:Key must be configured.");


// =====================================================
// AUTHENTICATION
// =====================================================
//
// API uses JWT Bearer.
// Cookie is only used for external Google/Facebook login.
// =====================================================

builder.Services
    .AddAuthentication(options =>
    {
        // JWT is the default authentication scheme
        // for API requests.
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })

    // -------------------------------------------------
    // JWT BEARER
    // -------------------------------------------------

    .AddJwtBearer(
        JwtBearerDefaults.AuthenticationScheme,
        options =>
        {
            options.TokenValidationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuer = true,

                    ValidateAudience = true,

                    ValidateLifetime = true,

                    ValidateIssuerSigningKey = true,

                    ValidIssuer =
                        builder.Configuration["Jwt:Issuer"],

                    ValidAudience =
                        builder.Configuration["Jwt:Audience"],

                    IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtKey)),

                    ClockSkew =
                        TimeSpan.Zero
                };
        })

    // -------------------------------------------------
    // COOKIE
    // -------------------------------------------------

    .AddCookie(
        CookieAuthenticationDefaults.AuthenticationScheme,
        options =>
        {
            options.Cookie.Name =
                "LadiesDress.External";

            options.ExpireTimeSpan =
                TimeSpan.FromMinutes(10);

            options.SlidingExpiration =
                false;
        });


// =====================================================
// AUTHORIZATION
// =====================================================

builder.Services.AddAuthorization();


// =====================================================
// CORS
// =====================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


// =====================================================
// BUILD
// =====================================================

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();


// =====================================================
// MIDDLEWARE
// =====================================================

app.UseHttpsRedirection();
app.UseStaticFiles();
var uploadsPath = builder.Configuration["UploadsPath"] ?? @"C:\UbhayaFashions\Uploads";
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.GetFullPath(uploadsPath)),
    RequestPath = "/uploads"
});

app.UseCors("AllowFrontend");

app.UseRouting();


// =====================================================
// IMPORTANT AUTH ORDER
// =====================================================
//
// Authentication MUST come BEFORE Authorization.
// =====================================================

app.UseAuthentication();

app.UseAuthorization();


// =====================================================
// CONTROLLERS / API ROUTES
// =====================================================

app.MapControllers();


// =====================================================
// DATABASE
// =====================================================

using (var scope = app.Services.CreateScope())
{
    var db =
        scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

    db.Database.ExecuteSqlRaw("""
        IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.Categories
            (
                Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
                Name nvarchar(250) NOT NULL,
                ImageUrl nvarchar(2048) NOT NULL CONSTRAINT DF_Categories_ImageUrl DEFAULT N'',
                ShowOnHomePage bit NOT NULL CONSTRAINT DF_Categories_ShowOnHomePage DEFAULT 0
            );
        END;

        IF COL_LENGTH(N'dbo.Categories', N'ImageUrl') IS NULL
            ALTER TABLE dbo.Categories ADD ImageUrl nvarchar(2048) NOT NULL CONSTRAINT DF_Categories_ImageUrl DEFAULT N'';
        IF COL_LENGTH(N'dbo.Categories', N'ShowOnHomePage') IS NULL
            ALTER TABLE dbo.Categories ADD ShowOnHomePage bit NOT NULL CONSTRAINT DF_Categories_ShowOnHomePage DEFAULT 0;

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Categories_Name' AND object_id = OBJECT_ID(N'dbo.Categories'))
            CREATE UNIQUE INDEX IX_Categories_Name ON dbo.Categories(Name);

        IF COL_LENGTH(N'dbo.Products', N'Category') IS NOT NULL
            EXEC(N'
                INSERT INTO dbo.Categories(Name)
                SELECT DISTINCT source.Name
                FROM dbo.Products product
                CROSS APPLY (SELECT CONVERT(nvarchar(250), LTRIM(RTRIM(product.Category))) AS Name) source
                WHERE source.Name <> N''''
                  AND NOT EXISTS (SELECT 1 FROM dbo.Categories category WHERE category.Name = source.Name);
            ');

        IF COL_LENGTH(N'dbo.Products', N'CategoryId') IS NULL
            ALTER TABLE dbo.Products ADD CategoryId int NULL;

        IF COL_LENGTH(N'dbo.Products', N'Category') IS NOT NULL
            EXEC(N'
                UPDATE product
                SET CategoryId = category.Id
                FROM dbo.Products product
                INNER JOIN dbo.Categories category
                    ON category.Name = CONVERT(nvarchar(250), LTRIM(RTRIM(product.Category)))
                WHERE product.CategoryId IS NULL;
            ');

        EXEC(N'
            IF EXISTS (SELECT 1 FROM dbo.Products WHERE CategoryId IS NULL)
               AND NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Name = N''Uncategorized'')
                INSERT INTO dbo.Categories(Name) VALUES (N''Uncategorized'');

            UPDATE product
            SET CategoryId = category.Id
            FROM dbo.Products product
            CROSS JOIN dbo.Categories category
            WHERE product.CategoryId IS NULL AND category.Name = N''Uncategorized'';
        ');

        EXEC(N'
            ALTER TABLE dbo.Products ALTER COLUMN CategoryId int NOT NULL;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N''IX_Products_CategoryId'' AND object_id = OBJECT_ID(N''dbo.Products''))
                CREATE INDEX IX_Products_CategoryId ON dbo.Products(CategoryId);

            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N''FK_Products_Categories_CategoryId'')
                ALTER TABLE dbo.Products ADD CONSTRAINT FK_Products_Categories_CategoryId
                    FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(Id) ON DELETE NO ACTION;
        ');

        IF COL_LENGTH(N'dbo.Products', N'Category') IS NOT NULL
            EXEC(N'ALTER TABLE dbo.Products DROP COLUMN Category;');
        """);

    // If you are using EF migrations:
    //
    // db.Database.Migrate();
    //
    // DO NOT use EnsureCreated()
    // when using EF migrations.
}


// =====================================================
// RUN
// =====================================================

app.Run();
