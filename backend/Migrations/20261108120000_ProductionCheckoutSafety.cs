using backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace backend.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261108120000_ProductionCheckoutSafety")]
public class ProductionCheckoutSafety : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("CheckoutKey", "Orders", type: "nvarchar(32)", maxLength: 32, nullable: true);
        migrationBuilder.AddColumn<string>("CheckoutFingerprint", "Orders", type: "nvarchar(64)", maxLength: 64, nullable: true);
        migrationBuilder.CreateIndex("IX_Orders_CustomerId_CheckoutKey", "Orders", new[] { "CustomerId", "CheckoutKey" }, unique: true, filter: "[CheckoutKey] IS NOT NULL");
        migrationBuilder.AddColumn<bool>("InventoryCommitted", "Orders", type: "bit", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<string>("FollowUpStatus", "Orders", type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Legacy");
        migrationBuilder.AddColumn<DateTime>("FollowUpAttemptAtUtc", "Orders", type: "datetime2", nullable: true);
        migrationBuilder.Sql("""
            UPDATE dbo.Orders SET InventoryCommitted = 1
            WHERE Status <> N'Cancelled' AND (PaymentMethod = N'COD' OR PaymentStatus = N'Paid');
            """);
        migrationBuilder.CreateIndex("IX_Orders_FollowUpStatus", "Orders", "FollowUpStatus");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("IX_Orders_CustomerId_CheckoutKey", "Orders");
        migrationBuilder.DropColumn("CheckoutKey", "Orders");
        migrationBuilder.DropColumn("CheckoutFingerprint", "Orders");
        migrationBuilder.DropIndex("IX_Orders_FollowUpStatus", "Orders");
        migrationBuilder.DropColumn("FollowUpAttemptAtUtc", "Orders");
        migrationBuilder.DropColumn("FollowUpStatus", "Orders");
        migrationBuilder.DropColumn("InventoryCommitted", "Orders");
    }
}
