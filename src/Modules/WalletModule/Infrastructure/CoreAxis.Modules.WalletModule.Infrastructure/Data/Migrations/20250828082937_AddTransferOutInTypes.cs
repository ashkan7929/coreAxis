using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CoreAxis.Modules.WalletModule.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferOutInTypes : Migration
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

            migrationBuilder.InsertData(
                schema: "wallet",
                table: "TransactionTypes",
                columns: new[] { "Id", "Code", "CreatedBy", "CreatedOn", "Description", "IsActive", "LastModifiedBy", "LastModifiedOn", "Name" },
                values: new object[,]
                {
                    { new Guid("44444444-4444-4444-4444-444444444444"), "TRANSFER_OUT", "System", new DateTime(2025, 8, 28, 8, 29, 37, 614, DateTimeKind.Utc).AddTicks(3690), "Transfer out transaction type for debiting funds from source wallet", true, "System", null, "Transfer Out" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "TRANSFER_IN", "System", new DateTime(2025, 8, 28, 8, 29, 37, 614, DateTimeKind.Utc).AddTicks(3690), "Transfer in transaction type for crediting funds to destination wallet", true, "System", null, "Transfer In" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "wallet",
                table: "TransactionTypes",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"));

            migrationBuilder.DeleteData(
                schema: "wallet",
                table: "TransactionTypes",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"));

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
    }
}
