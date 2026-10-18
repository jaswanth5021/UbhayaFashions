using backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261018090000_AddColorToProductVariants")]
public partial class AddColorToProductVariants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Color",
            table: "ProductVariants",
            type: "nvarchar(450)",
            nullable: false,
            defaultValue: "");

        migrationBuilder.DropIndex(
            name: "IX_ProductVariants_ProductId_Size",
            table: "ProductVariants");

        migrationBuilder.CreateIndex(
            name: "IX_ProductVariants_ProductId_Color_Size",
            table: "ProductVariants",
            columns: new[] { "ProductId", "Color", "Size" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ProductVariants_ProductId_Color_Size",
            table: "ProductVariants");

        migrationBuilder.DropColumn(
            name: "Color",
            table: "ProductVariants");

        migrationBuilder.CreateIndex(
            name: "IX_ProductVariants_ProductId_Size",
            table: "ProductVariants",
            columns: new[] { "ProductId", "Size" },
            unique: true);
    }
}
