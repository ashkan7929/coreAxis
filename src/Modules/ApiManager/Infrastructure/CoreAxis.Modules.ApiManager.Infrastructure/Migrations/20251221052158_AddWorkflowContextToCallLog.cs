using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAxis.Modules.ApiManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowContextToCallLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EndpointConfigJson",
                schema: "ApiManager",
                table: "WebServiceMethods",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StepId",
                schema: "ApiManager",
                table: "WebServiceCallLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkflowRunId",
                schema: "ApiManager",
                table: "WebServiceCallLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                schema: "ApiManager",
                table: "SecurityProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndpointConfigJson",
                schema: "ApiManager",
                table: "WebServiceMethods");

            migrationBuilder.DropColumn(
                name: "StepId",
                schema: "ApiManager",
                table: "WebServiceCallLogs");

            migrationBuilder.DropColumn(
                name: "WorkflowRunId",
                schema: "ApiManager",
                table: "WebServiceCallLogs");

            migrationBuilder.DropColumn(
                name: "Name",
                schema: "ApiManager",
                table: "SecurityProfiles");
        }
    }
}
