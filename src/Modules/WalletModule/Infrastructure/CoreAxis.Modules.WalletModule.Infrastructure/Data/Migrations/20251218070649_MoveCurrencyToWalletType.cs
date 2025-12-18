using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAxis.Modules.WalletModule.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MoveCurrencyToWalletType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                schema: "wallet",
                table: "Wallets");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                schema: "wallet",
                table: "WalletTypes",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "USD");

            migrationBuilder.UpdateData(
                schema: "wallet",
                table: "TransactionTypes",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedOn",
                value: new DateTime(2025, 12, 18, 7, 6, 49, 217, DateTimeKind.Utc).AddTicks(5070));

            migrationBuilder.UpdateData(
                schema: "wallet",
                table: "TransactionTypes",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedOn",
                value: new DateTime(2025, 12, 18, 7, 6, 49, 217, DateTimeKind.Utc).AddTicks(5070));

            migrationBuilder.UpdateData(
                schema: "wallet",
                table: "TransactionTypes",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedOn",
                value: new DateTime(2025, 12, 18, 7, 6, 49, 217, DateTimeKind.Utc).AddTicks(5070));

            migrationBuilder.UpdateData(
                schema: "wallet",
                table: "TransactionTypes",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedOn",
                value: new DateTime(2025, 12, 18, 7, 6, 49, 217, DateTimeKind.Utc).AddTicks(5070));

            migrationBuilder.UpdateData(
                schema: "wallet",
                table: "TransactionTypes",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "CreatedOn",
                value: new DateTime(2025, 12, 18, 7, 6, 49, 217, DateTimeKind.Utc).AddTicks(5070));

            migrationBuilder.InsertData(
                schema: "wallet",
                table: "TransactionTypes",
                columns: new[] { "Id", "Code", "CreatedBy", "CreatedOn", "Description", "IsActive", "LastModifiedBy", "LastModifiedOn", "Name" },
                values: new object[] { new Guid("66666666-6666-6666-6666-666666666666"), "COMMISSION", "System", new DateTime(2025, 12, 18, 7, 6, 49, 217, DateTimeKind.Utc).AddTicks(5070), "Commission transaction type for MLM payouts", true, "System", null, "Commission" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "wallet",
                table: "TransactionTypes",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"));

            migrationBuilder.DropColumn(
                name: "Currency",
                schema: "wallet",
                table: "WalletTypes");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                schema: "wallet",
                table: "Wallets",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                schema: "wallet",
                table: "TransactionTypes",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedOn",
                value: new DateTime(2025, 8, 28, 8, 29, 37, 614, DateTimeKind.Utc).AddTicks(3690));

            migrationBuilder.UpdateData(
                schema: "wallet",
                table: "TransactionTypes",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedOn",
                value: new DateTime(2025, 8, 28, 8, 29, 37, 614, DateTimeKind.Utc).AddTicks(3690));

            migrationBuilder.UpdateData(
                schema: "wallet",
                table: "TransactionTypes",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedOn",
                value: new DateTime(2025, 8, 28, 8, 29, 37, 614, DateTimeKind.Utc).AddTicks(3690));

            migrationBuilder.UpdateData(
                schema: "wallet",
                table: "TransactionTypes",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedOn",
                value: new DateTime(2025, 8, 28, 8, 29, 37, 614, DateTimeKind.Utc).AddTicks(3690));

            migrationBuilder.UpdateData(
                schema: "wallet",
                table: "TransactionTypes",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "CreatedOn",
                value: new DateTime(2025, 8, 28, 8, 29, 37, 614, DateTimeKind.Utc).AddTicks(3690));
        }
    }
}
