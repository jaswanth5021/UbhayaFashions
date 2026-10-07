using backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261007120000_AddProductCreatedAndUpdatedDates")]
public partial class AddProductCreatedAndUpdatedDates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH(N'dbo.Products', N'CreatedDate') IS NULL
            BEGIN
                ALTER TABLE dbo.Products ADD CreatedDate datetime2 NOT NULL
                    CONSTRAINT DF_Products_CreatedDate DEFAULT (SYSUTCDATETIME());
                -- Existing products have no stored launch date. Keep them out
                -- of the arrivals window until they are explicitly republished.
                EXEC(N'UPDATE dbo.Products SET CreatedDate = DATEADD(day, -31, SYSUTCDATETIME());');
            END;

            IF COL_LENGTH(N'dbo.Products', N'UpdatedDate') IS NULL
                ALTER TABLE dbo.Products ADD UpdatedDate datetime2 NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH(N'dbo.Products', N'UpdatedDate') IS NOT NULL
                ALTER TABLE dbo.Products DROP COLUMN UpdatedDate;
            IF COL_LENGTH(N'dbo.Products', N'CreatedDate') IS NOT NULL
            BEGIN
                ALTER TABLE dbo.Products DROP CONSTRAINT DF_Products_CreatedDate;
                ALTER TABLE dbo.Products DROP COLUMN CreatedDate;
            END;
            """);
    }
}
