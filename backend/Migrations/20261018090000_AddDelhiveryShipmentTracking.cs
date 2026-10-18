using System;
using backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261018090000_AddDelhiveryShipmentTracking")]
public partial class AddDelhiveryShipmentTracking : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "PaymentMethod", table: "Orders",
            type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "COD");

        migrationBuilder.CreateTable(
            name: "DelhiveryShipments",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                OrderId = table.Column<int>(type: "int", nullable: false),
                Waybill = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                ShipmentStatus = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                LabelUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                PickupLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                RecipientName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                RecipientPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                PostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                Country = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                WeightGrams = table.Column<int>(type: "int", nullable: false),
                AttemptCount = table.Column<int>(type: "int", nullable: false),
                LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DelhiveryShipments", x => x.Id);
                table.ForeignKey(name: "FK_DelhiveryShipments_Orders_OrderId", x => x.OrderId,
                    principalTable: "Orders", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_DelhiveryShipments_OrderId", table: "DelhiveryShipments",
            column: "OrderId", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "DelhiveryShipments");
        migrationBuilder.DropColumn(name: "PaymentMethod", table: "Orders");
    }
}