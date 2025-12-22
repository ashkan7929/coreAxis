using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAxis.Modules.DynamicForm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixDynamicFormSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SessionId",
                schema: "dynamicform",
                table: "FormSubmissions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BusinessId",
                schema: "dynamicform",
                table: "Forms",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SessionId",
                schema: "dynamicform",
                table: "FormSubmissions");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                schema: "dynamicform",
                table: "Forms");
        }
    }
}
