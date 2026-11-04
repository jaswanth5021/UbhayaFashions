using System;
using backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261104170000_AddRazorpayWebhookEvents")]
public partial class AddRazorpayWebhookEvents : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "DeliveryAddressJson",
            table: "Orders",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "RazorpayWebhookEvents",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                EventId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                RazorpayPaymentId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                RazorpayOrderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                ProcessingStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RazorpayWebhookEvents", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_RazorpayWebhookEvents_EventId",
            table: "RazorpayWebhookEvents",
            column: "EventId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "RazorpayWebhookEvents");
        migrationBuilder.DropColumn(name: "DeliveryAddressJson", table: "Orders");
    }
}
