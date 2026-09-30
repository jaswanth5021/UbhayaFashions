using backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260928190000_ReplaceProductCategoryWithForeignKey")]
public partial class ReplaceProductCategoryWithForeignKey : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Categories
                (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
                    Name nvarchar(250) NOT NULL
                );
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Categories_Name' AND object_id = OBJECT_ID(N'dbo.Categories'))
                CREATE UNIQUE INDEX IX_Categories_Name ON dbo.Categories(Name);

            IF COL_LENGTH(N'dbo.Products', N'Category') IS NOT NULL
                EXEC(N'
                    INSERT INTO dbo.Categories(Name)
                    SELECT DISTINCT source.Name
                    FROM dbo.Products product
                    CROSS APPLY (SELECT CONVERT(nvarchar(250), LTRIM(RTRIM(product.Category))) AS Name) source
                    WHERE source.Name <> N''''
                      AND NOT EXISTS (SELECT 1 FROM dbo.Categories category WHERE category.Name = source.Name);
                ');

            IF COL_LENGTH(N'dbo.Products', N'CategoryId') IS NULL
                ALTER TABLE dbo.Products ADD CategoryId int NULL;

            IF COL_LENGTH(N'dbo.Products', N'Category') IS NOT NULL
                EXEC(N'
                    UPDATE product
                    SET CategoryId = category.Id
                    FROM dbo.Products product
                    INNER JOIN dbo.Categories category
                        ON category.Name = CONVERT(nvarchar(250), LTRIM(RTRIM(product.Category)))
                    WHERE product.CategoryId IS NULL;
                ');

            EXEC(N'
                IF EXISTS (SELECT 1 FROM dbo.Products WHERE CategoryId IS NULL)
                   AND NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Name = N''Uncategorized'')
                    INSERT INTO dbo.Categories(Name) VALUES (N''Uncategorized'');

                UPDATE product
                SET CategoryId = category.Id
                FROM dbo.Products product
                CROSS JOIN dbo.Categories category
                WHERE product.CategoryId IS NULL AND category.Name = N''Uncategorized'';

                ALTER TABLE dbo.Products ALTER COLUMN CategoryId int NOT NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N''IX_Products_CategoryId'' AND object_id = OBJECT_ID(N''dbo.Products''))
                    CREATE INDEX IX_Products_CategoryId ON dbo.Products(CategoryId);

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N''FK_Products_Categories_CategoryId'')
                    ALTER TABLE dbo.Products ADD CONSTRAINT FK_Products_Categories_CategoryId
                        FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(Id) ON DELETE NO ACTION;
            ');

            IF COL_LENGTH(N'dbo.Products', N'Category') IS NOT NULL
                EXEC(N'ALTER TABLE dbo.Products DROP COLUMN Category;');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH(N'dbo.Products', N'Category') IS NULL
                ALTER TABLE dbo.Products ADD Category nvarchar(max) NOT NULL CONSTRAINT DF_Products_Category_Rollback DEFAULT N'';

            IF COL_LENGTH(N'dbo.Products', N'CategoryId') IS NOT NULL
                UPDATE product SET Category = category.Name
                FROM dbo.Products product INNER JOIN dbo.Categories category ON category.Id = product.CategoryId;

            IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Products_Categories_CategoryId')
                ALTER TABLE dbo.Products DROP CONSTRAINT FK_Products_Categories_CategoryId;
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_CategoryId' AND object_id = OBJECT_ID(N'dbo.Products'))
                DROP INDEX IX_Products_CategoryId ON dbo.Products;
            IF COL_LENGTH(N'dbo.Products', N'CategoryId') IS NOT NULL
                ALTER TABLE dbo.Products DROP COLUMN CategoryId;
            IF OBJECT_ID(N'dbo.Categories', N'U') IS NOT NULL
                DROP TABLE dbo.Categories;
            IF OBJECT_ID(N'dbo.DF_Products_Category_Rollback', N'D') IS NOT NULL
                ALTER TABLE dbo.Products DROP CONSTRAINT DF_Products_Category_Rollback;
            """);
    }
}
