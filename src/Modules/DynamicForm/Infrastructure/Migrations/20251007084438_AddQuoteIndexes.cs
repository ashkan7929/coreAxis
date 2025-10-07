using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAxis.Modules.DynamicForm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dynamicform");

            migrationBuilder.EnsureSchema(
                name: "DynamicForm");

            migrationBuilder.CreateTable(
                name: "DataSources",
                schema: "dynamicform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ServiceName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EndpointName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CacheTtlSeconds = table.Column<int>(type: "int", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Forms",
                schema: "dynamicform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Schema = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    TenantId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsMultiStep = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Forms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FormulaDefinitions",
                schema: "dynamicform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Expression = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    ReturnType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Parameters = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsDeprecated = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValidationRules = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Examples = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PerformanceMetrics = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dependencies = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormulaDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "dynamicform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(max)", maxLength: 1000, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MaxRetries = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                    NextRetryAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", maxLength: 100, nullable: false),
                    CausationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Quotes",
                schema: "dynamicform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Consumed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PricingJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InputsSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExternalDataSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FormAccessPolicies",
                schema: "dynamicform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PolicyType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TargetId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TargetName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Permissions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Conditions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormAccessPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormAccessPolicies_Forms_FormId",
                        column: x => x.FormId,
                        principalSchema: "dynamicform",
                        principalTable: "Forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FormFields",
                schema: "dynamicform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    FieldType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DefaultValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Placeholder = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HelpText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ValidationRules = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Options = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConditionalLogic = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CssClasses = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Attributes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CalculationExpression = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsCalculated = table.Column<bool>(type: "bit", nullable: false),
                    IsReadOnly = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormFields_Forms_FormId",
                        column: x => x.FormId,
                        principalSchema: "dynamicform",
                        principalTable: "Forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FormSteps",
                schema: "dynamicform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepNumber = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    StepSchema = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValidationRules = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConditionalLogic = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CanSkip = table.Column<bool>(type: "bit", nullable: false),
                    IsRepeatable = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsSkippable = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    StepType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Standard"),
                    MinTimeSeconds = table.Column<int>(type: "int", nullable: true),
                    MaxTimeSeconds = table.Column<int>(type: "int", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DependsOnSteps = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormSteps", x => x.Id);
                    table.CheckConstraint("CK_FormSteps_MaxAttempts", "[MaxAttempts] > 0");
                    table.CheckConstraint("CK_FormSteps_StepNumber", "[StepNumber] > 0");
                    table.CheckConstraint("CK_FormSteps_TimeoutMinutes", "[TimeoutMinutes] IS NULL OR [TimeoutMinutes] > 0");
                    table.ForeignKey(
                        name: "FK_FormSteps_Forms_FormId",
                        column: x => x.FormId,
                        principalSchema: "dynamicform",
                        principalTable: "Forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FormSubmissions",
                schema: "dynamicform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                    ValidationErrors = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Referrer = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentStepNumber = table.Column<int>(type: "int", nullable: true),
                    IsMultiStep = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormSubmissions_Forms_FormId",
                        column: x => x.FormId,
                        principalSchema: "dynamicform",
                        principalTable: "Forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FormVersions",
                schema: "dynamicform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", maxLength: 20, nullable: false),
                    Schema = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ChangeLog = table.Column<string>(type: "nvarchar(max)", maxLength: 2000, nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormVersions_Forms_FormId",
                        column: x => x.FormId,
                        principalSchema: "dynamicform",
                        principalTable: "Forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FormulaVersions",
                schema: "DynamicForm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormulaDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Expression = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ChangeLog = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidationRules = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    Dependencies = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    ExecutionCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AverageExecutionTime = table.Column<double>(type: "float(18)", precision: 18, scale: 6, nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LastErrorAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormulaVersions", x => x.Id);
                    table.CheckConstraint("CK_FormulaVersions_ActiveConstraint", "([IsActive] = 0) OR ([IsActive] = 1 AND [IsPublished] = 1)");
                    table.CheckConstraint("CK_FormulaVersions_AverageExecutionTime", "[AverageExecutionTime] IS NULL OR [AverageExecutionTime] >= 0");
                    table.CheckConstraint("CK_FormulaVersions_EffectiveRange", "([EffectiveFrom] IS NULL) OR ([EffectiveTo] IS NULL) OR ([EffectiveFrom] <= [EffectiveTo])");
                    table.CheckConstraint("CK_FormulaVersions_ExecutionCount", "[ExecutionCount] >= 0");
                    table.CheckConstraint("CK_FormulaVersions_PublishedConstraint", "([IsPublished] = 0) OR ([IsPublished] = 1 AND [PublishedAt] IS NOT NULL AND [PublishedBy] IS NOT NULL)");
                    table.CheckConstraint("CK_FormulaVersions_VersionNumber", "[VersionNumber] > 0");
                    table.ForeignKey(
                        name: "FK_FormulaVersions_FormulaDefinitions_FormulaDefinitionId",
                        column: x => x.FormulaDefinitionId,
                        principalSchema: "dynamicform",
                        principalTable: "FormulaDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductFormulaBindings",
                schema: "dynamicform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormulaDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FormulaDefinitionId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductFormulaBindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductFormulaBindings_FormulaDefinitions_FormulaDefinitionId",
                        column: x => x.FormulaDefinitionId,
                        principalSchema: "dynamicform",
                        principalTable: "FormulaDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductFormulaBindings_FormulaDefinitions_FormulaDefinitionId1",
                        column: x => x.FormulaDefinitionId1,
                        principalSchema: "dynamicform",
                        principalTable: "FormulaDefinitions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FormAuditLogs",
                schema: "dynamicform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FormSubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Changes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Information"),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "General"),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormAuditLogs_FormSubmissions_FormSubmissionId",
                        column: x => x.FormSubmissionId,
                        principalSchema: "dynamicform",
                        principalTable: "FormSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FormAuditLogs_Forms_FormId",
                        column: x => x.FormId,
                        principalSchema: "dynamicform",
                        principalTable: "Forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FormStepSubmissions",
                schema: "dynamicform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormSubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepNumber = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", maxLength: 100, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StepData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "NotStarted"),
                    ValidationErrors = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TimeSpentSeconds = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsSkipped = table.Column<bool>(type: "bit", nullable: false),
                    SkipReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormStepSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormStepSubmissions_FormSteps_FormStepId",
                        column: x => x.FormStepId,
                        principalSchema: "dynamicform",
                        principalTable: "FormSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormStepSubmissions_FormSubmissions_FormSubmissionId",
                        column: x => x.FormSubmissionId,
                        principalSchema: "dynamicform",
                        principalTable: "FormSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FormulaEvaluationLogs",
                schema: "dynamicform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormulaDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormulaVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContextId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContextType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InputParameters = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ErrorDetails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExecutionTimeMs = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    MemoryUsageBytes = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", maxLength: 100, nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EvaluationMode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FormId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FormSubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EvaluationContext = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", maxLength: 100, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormulaEvaluationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormulaEvaluationLogs_FormSubmissions_FormSubmissionId",
                        column: x => x.FormSubmissionId,
                        principalSchema: "dynamicform",
                        principalTable: "FormSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FormulaEvaluationLogs_Forms_FormId",
                        column: x => x.FormId,
                        principalSchema: "dynamicform",
                        principalTable: "Forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FormulaEvaluationLogs_FormulaDefinitions_FormulaDefinitionId",
                        column: x => x.FormulaDefinitionId,
                        principalSchema: "dynamicform",
                        principalTable: "FormulaDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FormulaEvaluationLogs_FormulaVersions_FormulaVersionId",
                        column: x => x.FormulaVersionId,
                        principalSchema: "DynamicForm",
                        principalTable: "FormulaVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataSource_Service_Endpoint",
                schema: "dynamicform",
                table: "DataSources",
                columns: new[] { "ServiceName", "EndpointName" });

            migrationBuilder.CreateIndex(
                name: "IX_FormAccessPolicies_EffectiveFrom",
                schema: "dynamicform",
                table: "FormAccessPolicies",
                column: "EffectiveFrom");

            migrationBuilder.CreateIndex(
                name: "IX_FormAccessPolicies_EffectiveTo",
                schema: "dynamicform",
                table: "FormAccessPolicies",
                column: "EffectiveTo");

            migrationBuilder.CreateIndex(
                name: "IX_FormAccessPolicies_FormId",
                schema: "dynamicform",
                table: "FormAccessPolicies",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_FormAccessPolicies_FormId_PolicyType_TargetId",
                schema: "dynamicform",
                table: "FormAccessPolicies",
                columns: new[] { "FormId", "PolicyType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_FormAccessPolicies_IsEnabled",
                schema: "dynamicform",
                table: "FormAccessPolicies",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_FormAccessPolicies_PolicyType",
                schema: "dynamicform",
                table: "FormAccessPolicies",
                column: "PolicyType");

            migrationBuilder.CreateIndex(
                name: "IX_FormAccessPolicies_Priority",
                schema: "dynamicform",
                table: "FormAccessPolicies",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_FormAccessPolicies_TargetId",
                schema: "dynamicform",
                table: "FormAccessPolicies",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_FormAccessPolicies_TenantId",
                schema: "dynamicform",
                table: "FormAccessPolicies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FormAccessPolicies_TenantId_FormId_Priority",
                schema: "dynamicform",
                table: "FormAccessPolicies",
                columns: new[] { "TenantId", "FormId", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_FormAuditLogs_Action",
                schema: "dynamicform",
                table: "FormAuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_FormAuditLogs_Category",
                schema: "dynamicform",
                table: "FormAuditLogs",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_FormAuditLogs_CorrelationId",
                schema: "dynamicform",
                table: "FormAuditLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_FormAuditLogs_EntityId",
                schema: "dynamicform",
                table: "FormAuditLogs",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_FormAuditLogs_EntityType",
                schema: "dynamicform",
                table: "FormAuditLogs",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_FormAuditLogs_EntityType_EntityId_Timestamp",
                schema: "dynamicform",
                table: "FormAuditLogs",
                columns: new[] { "EntityType", "EntityId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_FormAuditLogs_FormId",
                schema: "dynamicform",
                table: "FormAuditLogs",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_FormAuditLogs_FormSubmissionId",
                schema: "dynamicform",
                table: "FormAuditLogs",
                column: "FormSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormAuditLogs_SessionId",
                schema: "dynamicform",
                table: "FormAuditLogs",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormAuditLogs_Severity",
                schema: "dynamicform",
                table: "FormAuditLogs",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_FormAuditLogs_TenantId",
                schema: "dynamicform",
                table: "FormAuditLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FormAuditLogs_TenantId_Timestamp",
                schema: "dynamicform",
                table: "FormAuditLogs",
                columns: new[] { "TenantId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_FormAuditLogs_Timestamp",
                schema: "dynamicform",
                table: "FormAuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_FormAuditLogs_UserId",
                schema: "dynamicform",
                table: "FormAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FormAuditLogs_UserId_Action_Timestamp",
                schema: "dynamicform",
                table: "FormAuditLogs",
                columns: new[] { "UserId", "Action", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_FormFields_FieldType",
                schema: "dynamicform",
                table: "FormFields",
                column: "FieldType");

            migrationBuilder.CreateIndex(
                name: "IX_FormFields_FormId",
                schema: "dynamicform",
                table: "FormFields",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_FormFields_FormId_Name_Unique",
                schema: "dynamicform",
                table: "FormFields",
                columns: new[] { "FormId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormFields_FormId_Order",
                schema: "dynamicform",
                table: "FormFields",
                columns: new[] { "FormId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_FormFields_Name",
                schema: "dynamicform",
                table: "FormFields",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_FormFields_Order",
                schema: "dynamicform",
                table: "FormFields",
                column: "Order");

            migrationBuilder.CreateIndex(
                name: "IX_Forms_CreatedOn",
                schema: "dynamicform",
                table: "Forms",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Forms_IsPublished",
                schema: "dynamicform",
                table: "Forms",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_Forms_Name",
                schema: "dynamicform",
                table: "Forms",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Forms_TenantId",
                schema: "dynamicform",
                table: "Forms",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Forms_TenantId_Name_Unique",
                schema: "dynamicform",
                table: "Forms",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormSteps_CreatedOn",
                schema: "dynamicform",
                table: "FormSteps",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_FormSteps_FormId",
                schema: "dynamicform",
                table: "FormSteps",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_FormSteps_FormId_IsActive_StepNumber",
                schema: "dynamicform",
                table: "FormSteps",
                columns: new[] { "FormId", "IsActive", "StepNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_FormSteps_FormId_StepNumber_Unique",
                schema: "dynamicform",
                table: "FormSteps",
                columns: new[] { "FormId", "StepNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormSteps_IsRequired",
                schema: "dynamicform",
                table: "FormSteps",
                column: "IsRequired");

            migrationBuilder.CreateIndex(
                name: "IX_FormSteps_IsSkippable",
                schema: "dynamicform",
                table: "FormSteps",
                column: "IsSkippable");

            migrationBuilder.CreateIndex(
                name: "IX_FormSteps_StepNumber",
                schema: "dynamicform",
                table: "FormSteps",
                column: "StepNumber");

            migrationBuilder.CreateIndex(
                name: "IX_FormSteps_StepType",
                schema: "dynamicform",
                table: "FormSteps",
                column: "StepType");

            migrationBuilder.CreateIndex(
                name: "IX_FormSteps_TenantId",
                schema: "dynamicform",
                table: "FormSteps",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FormSteps_TenantId_FormId_IsActive",
                schema: "dynamicform",
                table: "FormSteps",
                columns: new[] { "TenantId", "FormId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FormSteps_TenantId_StepType_IsActive",
                schema: "dynamicform",
                table: "FormSteps",
                columns: new[] { "TenantId", "StepType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FormStepSubmissions_CompletedAt",
                schema: "dynamicform",
                table: "FormStepSubmissions",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FormStepSubmissions_CreatedOn",
                schema: "dynamicform",
                table: "FormStepSubmissions",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_FormStepSubmissions_FormStepId",
                schema: "dynamicform",
                table: "FormStepSubmissions",
                column: "FormStepId");

            migrationBuilder.CreateIndex(
                name: "IX_FormStepSubmissions_FormSubmissionId",
                schema: "dynamicform",
                table: "FormStepSubmissions",
                column: "FormSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormStepSubmissions_FormSubmissionId_Status_StepNumber",
                schema: "dynamicform",
                table: "FormStepSubmissions",
                columns: new[] { "FormSubmissionId", "Status", "StepNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_FormStepSubmissions_FormSubmissionId_StepNumber_Unique",
                schema: "dynamicform",
                table: "FormStepSubmissions",
                columns: new[] { "FormSubmissionId", "StepNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormStepSubmissions_StartedAt",
                schema: "dynamicform",
                table: "FormStepSubmissions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FormStepSubmissions_Status",
                schema: "dynamicform",
                table: "FormStepSubmissions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FormStepSubmissions_StepNumber",
                schema: "dynamicform",
                table: "FormStepSubmissions",
                column: "StepNumber");

            migrationBuilder.CreateIndex(
                name: "IX_FormStepSubmissions_TenantId",
                schema: "dynamicform",
                table: "FormStepSubmissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FormStepSubmissions_TenantId_FormSubmissionId_Status",
                schema: "dynamicform",
                table: "FormStepSubmissions",
                columns: new[] { "TenantId", "FormSubmissionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FormStepSubmissions_TenantId_UserId_Status",
                schema: "dynamicform",
                table: "FormStepSubmissions",
                columns: new[] { "TenantId", "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FormStepSubmissions_UserId",
                schema: "dynamicform",
                table: "FormStepSubmissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissions_FormId",
                schema: "dynamicform",
                table: "FormSubmissions",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissions_Status",
                schema: "dynamicform",
                table: "FormSubmissions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissions_SubmittedAt",
                schema: "dynamicform",
                table: "FormSubmissions",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissions_TenantId",
                schema: "dynamicform",
                table: "FormSubmissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissions_TenantId_FormId_SubmittedAt",
                schema: "dynamicform",
                table: "FormSubmissions",
                columns: new[] { "TenantId", "FormId", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FormulaDefinitions_Category",
                schema: "dynamicform",
                table: "FormulaDefinitions",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaDefinitions_IsPublished",
                schema: "dynamicform",
                table: "FormulaDefinitions",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaDefinitions_Name",
                schema: "dynamicform",
                table: "FormulaDefinitions",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaDefinitions_ReturnType",
                schema: "dynamicform",
                table: "FormulaDefinitions",
                column: "ReturnType");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaDefinitions_TenantId",
                schema: "dynamicform",
                table: "FormulaDefinitions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaDefinitions_TenantId_Category_IsPublished",
                schema: "dynamicform",
                table: "FormulaDefinitions",
                columns: new[] { "TenantId", "Category", "IsPublished" });

            migrationBuilder.CreateIndex(
                name: "IX_FormulaDefinitions_TenantId_Name_Unique",
                schema: "dynamicform",
                table: "FormulaDefinitions",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormulaDefinitions_Version",
                schema: "dynamicform",
                table: "FormulaDefinitions",
                column: "Version");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaEvaluationLogs_CompletedAt",
                schema: "dynamicform",
                table: "FormulaEvaluationLogs",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaEvaluationLogs_CorrelationId",
                schema: "dynamicform",
                table: "FormulaEvaluationLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaEvaluationLogs_EvaluationContext",
                schema: "dynamicform",
                table: "FormulaEvaluationLogs",
                column: "EvaluationContext");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaEvaluationLogs_ExecutionTimeMs",
                schema: "dynamicform",
                table: "FormulaEvaluationLogs",
                column: "ExecutionTimeMs");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaEvaluationLogs_FormId",
                schema: "dynamicform",
                table: "FormulaEvaluationLogs",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaEvaluationLogs_FormSubmissionId",
                schema: "dynamicform",
                table: "FormulaEvaluationLogs",
                column: "FormSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaEvaluationLogs_FormulaDefinitionId",
                schema: "dynamicform",
                table: "FormulaEvaluationLogs",
                column: "FormulaDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaEvaluationLogs_FormulaDefinitionId_Status_StartedAt",
                schema: "dynamicform",
                table: "FormulaEvaluationLogs",
                columns: new[] { "FormulaDefinitionId", "Status", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FormulaEvaluationLogs_FormulaVersionId",
                schema: "dynamicform",
                table: "FormulaEvaluationLogs",
                column: "FormulaVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaEvaluationLogs_SessionId",
                schema: "dynamicform",
                table: "FormulaEvaluationLogs",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaEvaluationLogs_StartedAt",
                schema: "dynamicform",
                table: "FormulaEvaluationLogs",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaEvaluationLogs_Status",
                schema: "dynamicform",
                table: "FormulaEvaluationLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaEvaluationLogs_TenantId",
                schema: "dynamicform",
                table: "FormulaEvaluationLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaEvaluationLogs_TenantId_StartedAt",
                schema: "dynamicform",
                table: "FormulaEvaluationLogs",
                columns: new[] { "TenantId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FormulaEvaluationLogs_UserId",
                schema: "dynamicform",
                table: "FormulaEvaluationLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaEvaluationLogs_UserId_StartedAt",
                schema: "dynamicform",
                table: "FormulaEvaluationLogs",
                columns: new[] { "UserId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FormulaVersions_FormulaDefinitionId",
                schema: "DynamicForm",
                table: "FormulaVersions",
                column: "FormulaDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaVersions_FormulaDefinitionId_IsActive_VersionNumber",
                schema: "DynamicForm",
                table: "FormulaVersions",
                columns: new[] { "FormulaDefinitionId", "IsActive", "VersionNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_FormulaVersions_FormulaDefinitionId_VersionNumber",
                schema: "DynamicForm",
                table: "FormulaVersions",
                columns: new[] { "FormulaDefinitionId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormulaVersions_IsActive",
                schema: "DynamicForm",
                table: "FormulaVersions",
                column: "IsActive",
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaVersions_IsPublished",
                schema: "DynamicForm",
                table: "FormulaVersions",
                column: "IsPublished",
                filter: "[IsPublished] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaVersions_LastExecutedAt",
                schema: "DynamicForm",
                table: "FormulaVersions",
                column: "LastExecutedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaVersions_Published_EffectiveWindow",
                schema: "DynamicForm",
                table: "FormulaVersions",
                columns: new[] { "FormulaDefinitionId", "IsPublished", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_FormulaVersions_PublishedAt",
                schema: "DynamicForm",
                table: "FormulaVersions",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaVersions_TenantId",
                schema: "DynamicForm",
                table: "FormulaVersions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FormulaVersions_TenantId_FormulaDefinitionId_IsActive",
                schema: "DynamicForm",
                table: "FormulaVersions",
                columns: new[] { "TenantId", "FormulaDefinitionId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FormulaVersions_TenantId_IsPublished_PublishedAt",
                schema: "DynamicForm",
                table: "FormulaVersions",
                columns: new[] { "TenantId", "IsPublished", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FormVersions_FormId",
                schema: "dynamicform",
                table: "FormVersions",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_FormVersions_FormId_IsCurrent",
                schema: "dynamicform",
                table: "FormVersions",
                columns: new[] { "FormId", "IsCurrent" },
                filter: "[IsCurrent] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_FormVersions_FormId_Version_Unique",
                schema: "dynamicform",
                table: "FormVersions",
                columns: new[] { "FormId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormVersions_IsCurrent",
                schema: "dynamicform",
                table: "FormVersions",
                column: "IsCurrent");

            migrationBuilder.CreateIndex(
                name: "IX_FormVersions_IsPublished",
                schema: "dynamicform",
                table: "FormVersions",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_FormVersions_PublishedAt",
                schema: "dynamicform",
                table: "FormVersions",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FormVersions_TenantId",
                schema: "dynamicform",
                table: "FormVersions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FormVersions_Version",
                schema: "dynamicform",
                table: "FormVersions",
                column: "Version");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_NextRetryAt",
                schema: "dynamicform",
                table: "OutboxMessages",
                column: "NextRetryAt");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_OccurredOn",
                schema: "dynamicform",
                table: "OutboxMessages",
                column: "OccurredOn");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOn",
                schema: "dynamicform",
                table: "OutboxMessages",
                column: "ProcessedOn");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOn_NextRetryAt",
                schema: "dynamicform",
                table: "OutboxMessages",
                columns: new[] { "ProcessedOn", "NextRetryAt" },
                filter: "[ProcessedOn] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_TenantId_OccurredOn",
                schema: "dynamicform",
                table: "OutboxMessages",
                columns: new[] { "TenantId", "OccurredOn" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Type",
                schema: "dynamicform",
                table: "OutboxMessages",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ProductFormulaBindings_FormulaDefinitionId",
                schema: "dynamicform",
                table: "ProductFormulaBindings",
                column: "FormulaDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductFormulaBindings_FormulaDefinitionId1",
                schema: "dynamicform",
                table: "ProductFormulaBindings",
                column: "FormulaDefinitionId1");

            migrationBuilder.CreateIndex(
                name: "UX_ProductFormulaBinding_ProductId_Version",
                schema: "dynamicform",
                table: "ProductFormulaBindings",
                columns: new[] { "ProductId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quote_Consumed",
                schema: "dynamicform",
                table: "Quotes",
                column: "Consumed");

            migrationBuilder.CreateIndex(
                name: "IX_Quote_ExpiresAt",
                schema: "dynamicform",
                table: "Quotes",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_Quote_Product_Consumed_Expires",
                schema: "dynamicform",
                table: "Quotes",
                columns: new[] { "ProductId", "Consumed", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Quote_ProductId",
                schema: "dynamicform",
                table: "Quotes",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataSources",
                schema: "dynamicform");

            migrationBuilder.DropTable(
                name: "FormAccessPolicies",
                schema: "dynamicform");

            migrationBuilder.DropTable(
                name: "FormAuditLogs",
                schema: "dynamicform");

            migrationBuilder.DropTable(
                name: "FormFields",
                schema: "dynamicform");

            migrationBuilder.DropTable(
                name: "FormStepSubmissions",
                schema: "dynamicform");

            migrationBuilder.DropTable(
                name: "FormulaEvaluationLogs",
                schema: "dynamicform");

            migrationBuilder.DropTable(
                name: "FormVersions",
                schema: "dynamicform");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "dynamicform");

            migrationBuilder.DropTable(
                name: "ProductFormulaBindings",
                schema: "dynamicform");

            migrationBuilder.DropTable(
                name: "Quotes",
                schema: "dynamicform");

            migrationBuilder.DropTable(
                name: "FormSteps",
                schema: "dynamicform");

            migrationBuilder.DropTable(
                name: "FormSubmissions",
                schema: "dynamicform");

            migrationBuilder.DropTable(
                name: "FormulaVersions",
                schema: "DynamicForm");

            migrationBuilder.DropTable(
                name: "Forms",
                schema: "dynamicform");

            migrationBuilder.DropTable(
                name: "FormulaDefinitions",
                schema: "dynamicform");
        }
    }
}
