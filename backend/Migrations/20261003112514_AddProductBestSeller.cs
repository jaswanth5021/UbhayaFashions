using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddProductBestSeller : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The API's startup schema bootstrap may have added this column
            // before EF records the migration in __EFMigrationsHistory.
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.Products', N'IsBestSeller') IS NULL
                    ALTER TABLE dbo.Products ADD IsBestSeller bit NOT NULL
                        CONSTRAINT DF_Products_IsBestSeller DEFAULT (0);
                """);
        }
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.Products', N'IsBestSeller') IS NOT NULL
                BEGIN
                    DECLARE @defaultConstraint sysname;
                    SELECT @defaultConstraint = dc.name
                    FROM sys.default_constraints dc
                    INNER JOIN sys.columns c
                        ON c.object_id = dc.parent_object_id
                       AND c.column_id = dc.parent_column_id
                    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Products')
                      AND c.name = N'IsBestSeller';

                    IF @defaultConstraint IS NOT NULL
                        EXEC(N'ALTER TABLE dbo.Products DROP CONSTRAINT ' + QUOTENAME(@defaultConstraint));

                    ALTER TABLE dbo.Products DROP COLUMN IsBestSeller;
                END;
                """);
        }
    }
}
