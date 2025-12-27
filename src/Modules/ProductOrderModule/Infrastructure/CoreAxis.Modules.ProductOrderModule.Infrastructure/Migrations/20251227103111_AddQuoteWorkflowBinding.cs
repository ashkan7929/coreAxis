using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAxis.Modules.ProductOrderModule.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteWorkflowBinding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuoteWorkflowBindings",
                schema: "productorder",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    AssetCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WorkflowCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WorkflowVersion = table.Column<int>(type: "int", nullable: false),
                    ReturnMappingSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteWorkflowBindings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuoteWorkflowBindings_AssetCode",
                schema: "productorder",
                table: "QuoteWorkflowBindings",
                column: "AssetCode");

            // Seed data
            migrationBuilder.InsertData(
                schema: "productorder",
                table: "QuoteWorkflowBindings",
                columns: new[] { "Id", "AssetCode", "WorkflowCode", "WorkflowVersion", "ReturnMappingSetId", "CreatedBy", "CreatedOn", "LastModifiedBy", "IsActive" },
                values: new object[] { Guid.NewGuid(), "1456", "mehraban-quote", 1, Guid.Empty, "System", DateTime.UtcNow, "System", true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuoteWorkflowBindings",
                schema: "productorder");
        }
    }
}
