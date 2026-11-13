using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using backend.Data;

#nullable disable

namespace backend.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261113120000_RestrictCouponScopeDeletion")]
public partial class RestrictCouponScopeDeletion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_Coupons_Products_ProductId", "Coupons");
        migrationBuilder.DropForeignKey("FK_Coupons_Categories_CategoryId", "Coupons");
        migrationBuilder.AddForeignKey("FK_Coupons_Products_ProductId", "Coupons", "ProductId", "Products", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey("FK_Coupons_Categories_CategoryId", "Coupons", "CategoryId", "Categories", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_Coupons_Products_ProductId", "Coupons");
        migrationBuilder.DropForeignKey("FK_Coupons_Categories_CategoryId", "Coupons");
        migrationBuilder.AddForeignKey("FK_Coupons_Products_ProductId", "Coupons", "ProductId", "Products", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
        migrationBuilder.AddForeignKey("FK_Coupons_Categories_CategoryId", "Coupons", "CategoryId", "Categories", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
    }
}
