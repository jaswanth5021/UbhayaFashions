using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations;

public partial class AddCouponDiscountManagement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>("DiscountAmount", "Orders", type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<string>("CouponCode", "Orders", type: "nvarchar(40)", maxLength: 40, nullable: true);
        migrationBuilder.AddColumn<int>("CouponId", "Orders", type: "int", nullable: true);

        migrationBuilder.CreateTable(
            name: "Coupons",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                DiscountType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                DiscountValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                MaximumDiscount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                MinimumOrderAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                MaximumUses = table.Column<int>(type: "int", nullable: true),
                UsageCount = table.Column<int>(type: "int", nullable: false),
                PerCustomerLimit = table.Column<int>(type: "int", nullable: true),
                StartsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                EndsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                IsAutomatic = table.Column<bool>(type: "bit", nullable: false),
                FirstOrderOnly = table.Column<bool>(type: "bit", nullable: false),
                ProductId = table.Column<int>(type: "int", nullable: true),
                CategoryId = table.Column<int>(type: "int", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Coupons", x => x.Id);
                table.ForeignKey("FK_Coupons_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_Coupons_Categories_CategoryId", x => x.CategoryId, "Categories", "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "CouponRedemptions",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                CouponId = table.Column<int>(type: "int", nullable: false),
                CustomerId = table.Column<int>(type: "int", nullable: false),
                OrderId = table.Column<int>(type: "int", nullable: false),
                RedeemedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CouponRedemptions", x => x.Id);
                table.ForeignKey("FK_CouponRedemptions_Coupons_CouponId", x => x.CouponId, "Coupons", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_CouponRedemptions_Customers_CustomerId", x => x.CustomerId, "Customers", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_CouponRedemptions_Orders_OrderId", x => x.OrderId, "Orders", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_Coupons_Code", "Coupons", "Code", unique: true);
        migrationBuilder.CreateIndex("IX_Coupons_ProductId", "Coupons", "ProductId");
        migrationBuilder.CreateIndex("IX_Coupons_CategoryId", "Coupons", "CategoryId");
        migrationBuilder.CreateIndex("IX_CouponRedemptions_CouponId_CustomerId_OrderId", "CouponRedemptions", new[] { "CouponId", "CustomerId", "OrderId" }, unique: true);
        migrationBuilder.CreateIndex("IX_CouponRedemptions_CustomerId_CouponId", "CouponRedemptions", new[] { "CustomerId", "CouponId" });
        migrationBuilder.CreateIndex("IX_CouponRedemptions_CustomerId", "CouponRedemptions", "CustomerId");
        migrationBuilder.CreateIndex("IX_CouponRedemptions_OrderId", "CouponRedemptions", "OrderId");
        migrationBuilder.AddForeignKey("FK_Orders_Coupons_CouponId", "Orders", "CouponId", "Coupons", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_Orders_Coupons_CouponId", "Orders");
        migrationBuilder.DropTable("CouponRedemptions");
        migrationBuilder.DropTable("Coupons");
        migrationBuilder.DropColumn("DiscountAmount", "Orders");
        migrationBuilder.DropColumn("CouponCode", "Orders");
        migrationBuilder.DropColumn("CouponId", "Orders");
    }
}