using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAxis.Modules.ProductOrderModule.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add SupplierId to Products if missing
            migrationBuilder.Sql(@"IF NOT EXISTS (
                SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = 'productorder' AND TABLE_NAME = 'Products' AND COLUMN_NAME = 'SupplierId'
            )
            BEGIN
                ALTER TABLE productorder.Products ADD SupplierId uniqueidentifier NULL;
            END");

            // Add Description to OrderLines if missing
            migrationBuilder.Sql(@"IF NOT EXISTS (
                SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = 'productorder' AND TABLE_NAME = 'OrderLines' AND COLUMN_NAME = 'Description'
            )
            BEGIN
                ALTER TABLE productorder.OrderLines ADD Description nvarchar(max) NULL;
            END");

            // Create IdempotencyEntries if missing
            migrationBuilder.Sql(@"IF NOT EXISTS (
                SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_SCHEMA='productorder' AND TABLE_NAME='IdempotencyEntries'
            )
            BEGIN
                CREATE TABLE productorder.IdempotencyEntries (
                    Id uniqueidentifier NOT NULL CONSTRAINT DF_IdempotencyEntries_Id DEFAULT NEWID(),
                    IdempotencyKey nvarchar(100) NOT NULL,
                    Operation nvarchar(100) NOT NULL,
                    RequestHash nvarchar(64) NOT NULL,
                    CreatedOn datetime2 NOT NULL CONSTRAINT DF_IdempotencyEntries_CreatedOn DEFAULT GETUTCDATE(),
                    CONSTRAINT PK_IdempotencyEntries PRIMARY KEY (Id)
                );
            END");

            // Create Suppliers if missing
            migrationBuilder.Sql(@"IF NOT EXISTS (
                SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_SCHEMA='productorder' AND TABLE_NAME='Suppliers'
            )
            BEGIN
                CREATE TABLE productorder.Suppliers (
                    Id uniqueidentifier NOT NULL CONSTRAINT DF_Suppliers_Id DEFAULT NEWID(),
                    Code nvarchar(100) NOT NULL,
                    Name nvarchar(200) NOT NULL,
                    CreatedBy nvarchar(256) NOT NULL,
                    CreatedOn datetime2 NOT NULL CONSTRAINT DF_Suppliers_CreatedOn DEFAULT GETUTCDATE(),
                    LastModifiedBy nvarchar(256) NOT NULL,
                    LastModifiedOn datetime2 NULL,
                    IsActive bit NOT NULL,
                    CONSTRAINT PK_Suppliers PRIMARY KEY (Id)
                );
            END");

            // Indexes (create only if missing)
            migrationBuilder.Sql(@"IF NOT EXISTS (
                SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_SupplierId' AND object_id = OBJECT_ID('productorder.Products')
            )
            BEGIN
                CREATE INDEX IX_Products_SupplierId ON productorder.Products (SupplierId);
            END");

            migrationBuilder.Sql(@"IF NOT EXISTS (
                SELECT 1 FROM sys.indexes WHERE name = 'IX_IdempotencyEntries_IdempotencyKey' AND object_id = OBJECT_ID('productorder.IdempotencyEntries')
            )
            BEGIN
                CREATE UNIQUE INDEX IX_IdempotencyEntries_IdempotencyKey ON productorder.IdempotencyEntries (IdempotencyKey);
            END");

            migrationBuilder.Sql(@"IF NOT EXISTS (
                SELECT 1 FROM sys.indexes WHERE name = 'IX_IdempotencyEntries_Operation_RequestHash' AND object_id = OBJECT_ID('productorder.IdempotencyEntries')
            )
            BEGIN
                CREATE INDEX IX_IdempotencyEntries_Operation_RequestHash ON productorder.IdempotencyEntries (Operation, RequestHash);
            END");

            migrationBuilder.Sql(@"IF NOT EXISTS (
                SELECT 1 FROM sys.indexes WHERE name = 'IX_Suppliers_Code' AND object_id = OBJECT_ID('productorder.Suppliers')
            )
            BEGIN
                CREATE UNIQUE INDEX IX_Suppliers_Code ON productorder.Suppliers (Code);
            END");

            migrationBuilder.Sql(@"IF NOT EXISTS (
                SELECT 1 FROM sys.indexes WHERE name = 'IX_Suppliers_Name' AND object_id = OBJECT_ID('productorder.Suppliers')
            )
            BEGIN
                CREATE INDEX IX_Suppliers_Name ON productorder.Suppliers (Name);
            END");

            // Foreign key from Products.SupplierId to Suppliers.Id (add only if missing)
            migrationBuilder.Sql(@"IF NOT EXISTS (
                SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Products_Suppliers_SupplierId' AND parent_object_id = OBJECT_ID('productorder.Products')
            )
            BEGIN
                ALTER TABLE productorder.Products
                ADD CONSTRAINT FK_Products_Suppliers_SupplierId
                FOREIGN KEY (SupplierId) REFERENCES productorder.Suppliers (Id)
                ON DELETE SET NULL;
            END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Suppliers_SupplierId",
                schema: "productorder",
                table: "Products");

            migrationBuilder.DropTable(
                name: "IdempotencyEntries",
                schema: "productorder");

            migrationBuilder.DropTable(
                name: "Suppliers",
                schema: "productorder");

            migrationBuilder.DropIndex(
                name: "IX_Products_SupplierId",
                schema: "productorder",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                schema: "productorder",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "productorder",
                table: "OrderLines");
        }
    }
}
