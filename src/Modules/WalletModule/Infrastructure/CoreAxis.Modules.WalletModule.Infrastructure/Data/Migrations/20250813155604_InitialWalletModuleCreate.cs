using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAxis.Modules.WalletModule.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialWalletModuleCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "wallet");

            migrationBuilder.CreateTable(
                name: "TransactionTypes",
                schema: "wallet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WalletProviders",
                schema: "wallet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ApiUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SupportsDeposit = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SupportsWithdraw = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Configuration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WalletTypes",
                schema: "wallet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Wallets",
                schema: "wallet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WalletTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    LockReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wallets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Wallets_WalletTypes_WalletTypeId",
                        column: x => x.WalletTypeId,
                        principalSchema: "wallet",
                        principalTable: "WalletTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                schema: "wallet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    WalletId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RelatedTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_TransactionTypes_TransactionTypeId",
                        column: x => x.TransactionTypeId,
                        principalSchema: "wallet",
                        principalTable: "TransactionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transactions_Transactions_RelatedTransactionId",
                        column: x => x.RelatedTransactionId,
                        principalSchema: "wallet",
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transactions_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalSchema: "wallet",
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WalletContracts",
                schema: "wallet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WalletId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaxAmount = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    DailyLimit = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    MonthlyLimit = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    UsedDailyAmount = table.Column<decimal>(type: "decimal(18,8)", nullable: false, defaultValue: 0m),
                    UsedMonthlyAmount = table.Column<decimal>(type: "decimal(18,8)", nullable: false, defaultValue: 0m),
                    LastResetDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Terms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WalletContracts_WalletProviders_ProviderId",
                        column: x => x.ProviderId,
                        principalSchema: "wallet",
                        principalTable: "WalletProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WalletContracts_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalSchema: "wallet",
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_CorrelationId",
                schema: "wallet",
                table: "Transactions",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_CreatedOn",
                schema: "wallet",
                table: "Transactions",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_IdempotencyKey",
                schema: "wallet",
                table: "Transactions",
                column: "IdempotencyKey",
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Reference",
                schema: "wallet",
                table: "Transactions",
                column: "Reference");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_RelatedTransactionId",
                schema: "wallet",
                table: "Transactions",
                column: "RelatedTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Status",
                schema: "wallet",
                table: "Transactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TransactionTypeId",
                schema: "wallet",
                table: "Transactions",
                column: "TransactionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_WalletId",
                schema: "wallet",
                table: "Transactions",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionTypes_Code",
                schema: "wallet",
                table: "TransactionTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransactionTypes_Name",
                schema: "wallet",
                table: "TransactionTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletContracts_ProviderId",
                schema: "wallet",
                table: "WalletContracts",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletContracts_UserId",
                schema: "wallet",
                table: "WalletContracts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletContracts_WalletId",
                schema: "wallet",
                table: "WalletContracts",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletContracts_WalletId_ProviderId",
                schema: "wallet",
                table: "WalletContracts",
                columns: new[] { "WalletId", "ProviderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletProviders_Name",
                schema: "wallet",
                table: "WalletProviders",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletProviders_Type",
                schema: "wallet",
                table: "WalletProviders",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_UserId",
                schema: "wallet",
                table: "Wallets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_UserId_WalletTypeId",
                schema: "wallet",
                table: "Wallets",
                columns: new[] { "UserId", "WalletTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_WalletTypeId",
                schema: "wallet",
                table: "Wallets",
                column: "WalletTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTypes_Name",
                schema: "wallet",
                table: "WalletTypes",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transactions",
                schema: "wallet");

            migrationBuilder.DropTable(
                name: "WalletContracts",
                schema: "wallet");

            migrationBuilder.DropTable(
                name: "TransactionTypes",
                schema: "wallet");

            migrationBuilder.DropTable(
                name: "WalletProviders",
                schema: "wallet");

            migrationBuilder.DropTable(
                name: "Wallets",
                schema: "wallet");

            migrationBuilder.DropTable(
                name: "WalletTypes",
                schema: "wallet");
        }
    }
}
