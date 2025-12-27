using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAxis.Modules.Workflow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMehrabanWorkflowSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var definitionId = new Guid("11111111-1111-1111-1111-111111111111");
            var versionId = new Guid("22222222-2222-2222-2222-222222222222");
            var dslJson = "{\"startAt\":\"getAppToken\",\"steps\":[{\"id\":\"getAppToken\",\"type\":\"apiCall\",\"config\":{\"apiMethodRef\":\"00000000-0000-0000-0000-000000000000\",\"inputMappingSetId\":\"00000000-0000-0000-0000-000000000000\",\"outputMappingSetId\":\"00000000-0000-0000-0000-000000000000\",\"saveStepIO\":true}},{\"id\":\"returnQuote\",\"type\":\"return\",\"config\":{\"outputMappingSetId\":\"00000000-0000-0000-0000-000000000000\"}}]}";

            migrationBuilder.InsertData(
                schema: "workflow",
                table: "WorkflowDefinitions",
                columns: new[] { "Id", "Code", "Name", "TenantId", "CreatedBy", "CreatedOn", "LastModifiedBy", "LastModifiedOn", "IsActive" },
                values: new object[] { definitionId, "mehraban-quote", "Mehraban Quote Workflow", "default", "System", DateTime.UtcNow, "System", DateTime.UtcNow, true });

            migrationBuilder.InsertData(
                schema: "workflow",
                table: "WorkflowDefinitionVersions",
                columns: new[] { "Id", "WorkflowDefinitionId", "VersionNumber", "Status", "DslJson", "CreatedBy", "CreatedOn", "LastModifiedBy", "LastModifiedOn", "IsActive" },
                values: new object[] { versionId, definitionId, 1, 1, dslJson, "System", DateTime.UtcNow, "System", DateTime.UtcNow, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var definitionId = new Guid("11111111-1111-1111-1111-111111111111");
            
            migrationBuilder.DeleteData(
                schema: "workflow",
                table: "WorkflowDefinitionVersions",
                keyColumn: "WorkflowDefinitionId",
                keyValue: definitionId);

            migrationBuilder.DeleteData(
                schema: "workflow",
                table: "WorkflowDefinitions",
                keyColumn: "Id",
                keyValue: definitionId);
        }
    }
}
