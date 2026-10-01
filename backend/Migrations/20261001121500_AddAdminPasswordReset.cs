using backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261001121500_AddAdminPasswordReset")]
public partial class AddAdminPasswordReset : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.AdminUsers', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.AdminUsers
                (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AdminUsers PRIMARY KEY,
                    Name nvarchar(max) NOT NULL,
                    Email nvarchar(450) NOT NULL,
                    PasswordHash nvarchar(max) NOT NULL,
                    PasswordResetTokenHash nvarchar(64) NULL,
                    PasswordResetExpiresUtc datetime2 NULL,
                    IsActive bit NOT NULL CONSTRAINT DF_AdminUsers_IsActive DEFAULT (1),
                    CreatedDate datetime2 NOT NULL CONSTRAINT DF_AdminUsers_CreatedDate DEFAULT (SYSUTCDATETIME())
                );
                CREATE UNIQUE INDEX IX_AdminUsers_Email ON dbo.AdminUsers(Email);
            END;

            IF COL_LENGTH(N'dbo.AdminUsers', N'PasswordResetTokenHash') IS NULL
                ALTER TABLE dbo.AdminUsers ADD PasswordResetTokenHash nvarchar(64) NULL;
            IF COL_LENGTH(N'dbo.AdminUsers', N'PasswordResetExpiresUtc') IS NULL
                ALTER TABLE dbo.AdminUsers ADD PasswordResetExpiresUtc datetime2 NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.AdminUsers', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH(N'dbo.AdminUsers', N'PasswordResetTokenHash') IS NOT NULL
                    ALTER TABLE dbo.AdminUsers DROP COLUMN PasswordResetTokenHash;
                IF COL_LENGTH(N'dbo.AdminUsers', N'PasswordResetExpiresUtc') IS NOT NULL
                    ALTER TABLE dbo.AdminUsers DROP COLUMN PasswordResetExpiresUtc;
            END;
            """);
    }
}
