using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddProductVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Size",
                table: "InventoryTransactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Size = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Stock = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariants_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_Size",
                table: "ProductVariants",
                columns: new[] { "ProductId", "Size" },
                unique: true);

            migrationBuilder.Sql("""
                ;WITH SplitSizes AS
                (
                    SELECT p.Id AS ProductId, p.Price, p.Discount, p.Stock,
                        CAST(LTRIM(RTRIM(LEFT(input.SizeText, CHARINDEX(',', input.SizeText + ',') - 1))) AS nvarchar(450)) AS Size,
                        CAST(STUFF(input.SizeText, 1, CHARINDEX(',', input.SizeText + ','), '') AS nvarchar(max)) AS Remaining,
                        CAST(1 AS int) AS Ordinal
                    FROM dbo.Products AS p
                    CROSS APPLY
                    (
                        SELECT CASE WHEN NULLIF(LTRIM(RTRIM(p.Sizes)), '') IS NULL
                                    THEN 'FREE SIZE' ELSE LTRIM(RTRIM(p.Sizes)) END AS SizeText
                    ) AS input

                    UNION ALL

                    SELECT ProductId, Price, Discount, Stock,
                        CAST(LTRIM(RTRIM(LEFT(Remaining, CHARINDEX(',', Remaining + ',') - 1))) AS nvarchar(450)),
                        CAST(STUFF(Remaining, 1, CHARINDEX(',', Remaining + ','), '') AS nvarchar(max)),
                        Ordinal + 1
                    FROM SplitSizes
                    WHERE Remaining <> ''
                ), DistinctSizes AS
                (
                    SELECT ProductId, Price, Discount, Stock,
                        CASE WHEN UPPER(LTRIM(RTRIM(Size))) = 'FS' THEN 'FREE SIZE'
                             ELSE UPPER(LTRIM(RTRIM(Size))) END AS Size,
                        Ordinal,
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY ProductId,
                                CASE WHEN UPPER(LTRIM(RTRIM(Size))) = 'FS' THEN 'FREE SIZE'
                                     ELSE UPPER(LTRIM(RTRIM(Size))) END
                            ORDER BY Ordinal
                        ) AS DuplicateRank
                    FROM SplitSizes
                    WHERE LTRIM(RTRIM(Size)) <> ''
                ), NumberedSizes AS
                (
                    SELECT ProductId, Price, Discount, CASE WHEN Stock < 0 THEN 0 ELSE Stock END AS Stock,
                        Size,
                        ROW_NUMBER() OVER (PARTITION BY ProductId ORDER BY Ordinal) AS VariantOrdinal,
                        COUNT(*) OVER (PARTITION BY ProductId) AS VariantCount
                    FROM DistinctSizes
                    WHERE DuplicateRank = 1
                )
                INSERT INTO dbo.ProductVariants (ProductId, Size, Price, Discount, Stock)
                SELECT ProductId, Size, Price, Discount,
                    (Stock / VariantCount) + CASE WHEN VariantOrdinal <= (Stock % VariantCount) THEN 1 ELSE 0 END
                FROM NumberedSizes
                OPTION (MAXRECURSION 100);
                """);

            migrationBuilder.DropColumn(name: "Discount", table: "Products");
            migrationBuilder.DropColumn(name: "Price", table: "Products");
            migrationBuilder.DropColumn(name: "Sizes", table: "Products");
            migrationBuilder.DropColumn(name: "Stock", table: "Products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "InventoryTransactions");

            migrationBuilder.AddColumn<decimal>(
                name: "Discount",
                table: "Products",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Products",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Sizes",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Stock",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
