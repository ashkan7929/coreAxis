using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAxis.Modules.ProductOrderModule.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialProductOrderModuleCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "productorder");

            migrationBuilder.CreateTable(
                name: "Orders",
                schema: "productorder",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrderType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AssetCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    LockedPriceAmount = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: true),
                    LockedPriceCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PriceLockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PriceExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: true),
                    TotalCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "default"),
                    JsonSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "productorder",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Type = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MaxRetries = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                    NextRetryAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CausationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "default"),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderLines",
                schema: "productorder",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderLineAssetCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    UnitPriceAmount = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: true),
                    UnitPriceCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    LineTotalAmount = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: true),
                    LineTotalCurrency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderLines_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "productorder",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_OrderId",
                schema: "productorder",
                table: "OrderLines",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CreatedOn",
                schema: "productorder",
                table: "Orders",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_IdempotencyKey",
                schema: "productorder",
                table: "Orders",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNumber",
                schema: "productorder",
                table: "Orders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PriceExpiresAt",
                schema: "productorder",
                table: "Orders",
                column: "PriceExpiresAt",
                filter: "[PriceExpiresAt] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TenantId",
                schema: "productorder",
                table: "Orders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId",
                schema: "productorder",
                table: "Orders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId_Status",
                schema: "productorder",
                table: "Orders",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_CorrelationId",
                schema: "productorder",
                table: "OutboxMessages",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Processing",
                schema: "productorder",
                table: "OutboxMessages",
                columns: new[] { "ProcessedOn", "NextRetryAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderLines",
                schema: "productorder");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "productorder");

            migrationBuilder.DropTable(
                name: "Orders",
                schema: "productorder");
        }
    }
}
