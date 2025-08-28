using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAxis.Modules.WalletModule.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferOutInTransactionTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "wallet",
                table: "TransactionTypes",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedOn",
                value: new DateTime(2025, 8, 28, 8, 29, 1, 370, DateTimeKind.Utc).AddTicks(1670));

            migrationBuilder.UpdateData(
                schema: "wallet",
                table: "TransactionTypes",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedOn",
                value: new DateTime(2025, 8, 28, 8, 29, 1, 370, DateTimeKind.Utc).AddTicks(1670));

            migrationBuilder.UpdateData(
                schema: "wallet",
                table: "TransactionTypes",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedOn",
                value: new DateTime(2025, 8, 28, 8, 29, 1, 370, DateTimeKind.Utc).AddTicks(1670));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "wallet",
                table: "TransactionTypes",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedOn",
                value: new DateTime(2025, 8, 28, 8, 16, 50, 800, DateTimeKind.Utc).AddTicks(7960));

            migrationBuilder.UpdateData(
                schema: "wallet",
                table: "TransactionTypes",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedOn",
                value: new DateTime(2025, 8, 28, 8, 16, 50, 800, DateTimeKind.Utc).AddTicks(7960));

            migrationBuilder.UpdateData(
                schema: "wallet",
                table: "TransactionTypes",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedOn",
                value: new DateTime(2025, 8, 28, 8, 16, 50, 800, DateTimeKind.Utc).AddTicks(7960));
        }
    }
}
