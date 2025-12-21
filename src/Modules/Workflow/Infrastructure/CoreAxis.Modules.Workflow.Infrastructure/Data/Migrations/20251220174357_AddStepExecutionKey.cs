using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAxis.Modules.Workflow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStepExecutionKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExecutionKey",
                schema: "workflow",
                table: "WorkflowRunSteps",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExecutionKey",
                schema: "workflow",
                table: "WorkflowRunSteps");
        }
    }
}
