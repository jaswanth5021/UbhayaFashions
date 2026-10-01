using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(
                        type: "int",
                        nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    Name = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: false),

                    Email = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: true),

                    Mobile = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: true),

                    Age = table.Column<int>(
                        type: "int",
                        nullable: true),

                    PasswordHash = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: true),

                    ProfileImage = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: true),

                    CreatedDate = table.Column<DateTime>(
                        type: "datetime2",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(
                        type: "int",
                        nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    Name = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: false),

                    Description = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: false),

                    Price = table.Column<decimal>(
                        type: "decimal(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: false),

                    Discount = table.Column<decimal>(
                        type: "decimal(5,2)",
                        precision: 5,
                        scale: 2,
                        nullable: false),

                    Stock = table.Column<int>(
                        type: "int",
                        nullable: false),

                    Category = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: false),

                    Sizes = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: false),

                    Colors = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: false),

                    ImageUrl = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            // InventoryTransactions is created in InitialCreate
            // because later migrations add columns to this table.
            migrationBuilder.CreateTable(
                name: "InventoryTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(
                        type: "int",
                        nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    ProductId = table.Column<int>(
                        type: "int",
                        nullable: false),

                    Type = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: false),

                    Quantity = table.Column<int>(
                        type: "int",
                        nullable: false),

                    StockAfter = table.Column<int>(
                        type: "int",
                        nullable: false),

                    Note = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: true),

                    CreatedDate = table.Column<DateTime>(
                        type: "datetime2",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_InventoryTransactions",
                        x => x.Id);

                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_ProductId",
                table: "InventoryTransactions",
                column: "ProductId");

            // AdminUsers is required by ApplicationDbContext
            // and AdminAuthController.
            migrationBuilder.CreateTable(
                name: "AdminUsers",
                columns: table => new
                {
                    Id = table.Column<int>(
                        type: "int",
                        nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    Name = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: false),

                    // SQL Server cannot index nvarchar(max).
                    // 320 characters is sufficient for email addresses.
                    Email = table.Column<string>(
                        type: "nvarchar(320)",
                        maxLength: 320,
                        nullable: false),

                    PasswordHash = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: false),

                    IsActive = table.Column<bool>(
                        type: "bit",
                        nullable: false),

                    CreatedDate = table.Column<DateTime>(
                        type: "datetime2",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_AdminUsers",
                        x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminUsers_Email",
                table: "AdminUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateTable(
                name: "ExternalLogins",
                columns: table => new
                {
                    Id = table.Column<int>(
                        type: "int",
                        nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    CustomerId = table.Column<int>(
                        type: "int",
                        nullable: false),

                    Provider = table.Column<string>(
                        type: "nvarchar(450)",
                        nullable: false),

                    ProviderUserId = table.Column<string>(
                        type: "nvarchar(450)",
                        nullable: false),

                    Email = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_ExternalLogins",
                        x => x.Id);

                    table.ForeignKey(
                        name: "FK_ExternalLogins_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(
                        type: "int",
                        nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    CustomerId = table.Column<int>(
                        type: "int",
                        nullable: false),

                    TotalAmount = table.Column<decimal>(
                        type: "decimal(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: false),

                    Status = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: false),

                    PaymentStatus = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: false),

                    ShippingAddress = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: false),

                    CreatedDate = table.Column<DateTime>(
                        type: "datetime2",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_Orders",
                        x => x.Id);

                    table.ForeignKey(
                        name: "FK_Orders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(
                        type: "int",
                        nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    OrderId = table.Column<int>(
                        type: "int",
                        nullable: false),

                    ProductId = table.Column<int>(
                        type: "int",
                        nullable: false),

                    ProductName = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: false),

                    Price = table.Column<decimal>(
                        type: "decimal(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: false),

                    Quantity = table.Column<int>(
                        type: "int",
                        nullable: false),

                    Size = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: true),

                    Color = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_OrderItems",
                        x => x.Id);

                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLogins_CustomerId",
                table: "ExternalLogins",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLogins_Provider_ProviderUserId",
                table: "ExternalLogins",
                columns: new[] { "Provider", "ProviderUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders",
                column: "CustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalLogins");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "AdminUsers");

            migrationBuilder.DropTable(
                name: "InventoryTransactions");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Customers");
        }
    }
}