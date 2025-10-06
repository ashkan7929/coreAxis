using System;
using Microsoft.EntityFrameworkCore.Migrations;
using CoreAxis.Modules.ProductOrderModule.Infrastructure.Data;

namespace CoreAxis.Modules.ProductOrderModule.Infrastructure.Migrations
{
    [Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(ProductOrderDbContext))]
    [Migration("20251006120000_AddIdempotencyEntries")]
    public partial class AddIdempotencyEntries : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IdempotencyEntries",
                schema: "productorder",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyEntries_IdempotencyKey",
                schema: "productorder",
                table: "IdempotencyEntries",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyEntries_Operation_RequestHash",
                schema: "productorder",
                table: "IdempotencyEntries",
                columns: new[] { "Operation", "RequestHash" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdempotencyEntries",
                schema: "productorder");
        }
    }
}