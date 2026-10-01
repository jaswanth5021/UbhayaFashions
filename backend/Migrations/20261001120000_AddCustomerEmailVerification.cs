using backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261001120000_AddCustomerEmailVerification")]
public partial class AddCustomerEmailVerification : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(name: "EmailVerified", table: "Customers", type: "bit", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<string>(name: "EmailVerificationCodeHash", table: "Customers", type: "nvarchar(64)", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "EmailVerificationCodeExpiresUtc", table: "Customers", type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "EmailVerificationLastSentUtc", table: "Customers", type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<int>(name: "EmailVerificationFailedAttempts", table: "Customers", type: "int", nullable: false, defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "EmailVerified", table: "Customers");
        migrationBuilder.DropColumn(name: "EmailVerificationCodeHash", table: "Customers");
        migrationBuilder.DropColumn(name: "EmailVerificationCodeExpiresUtc", table: "Customers");
        migrationBuilder.DropColumn(name: "EmailVerificationLastSentUtc", table: "Customers");
        migrationBuilder.DropColumn(name: "EmailVerificationFailedAttempts", table: "Customers");
    }
}
