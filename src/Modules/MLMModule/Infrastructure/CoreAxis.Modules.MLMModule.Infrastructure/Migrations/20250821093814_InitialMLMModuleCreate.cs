using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAxis.Modules.MLMModule.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialMLMModuleCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "mlm");

            migrationBuilder.CreateTable(
                name: "CommissionRuleSets",
                schema: "mlm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    LatestVersion = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    MaxLevels = table.Column<int>(type: "int", nullable: false, defaultValue: 10),
                    MinimumPurchaseAmount = table.Column<decimal>(type: "decimal(18,6)", nullable: false, defaultValue: 0m),
                    RequireActiveUpline = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionRuleSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "mlm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MaxRetries = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                    NextRetryAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CausationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "default"),
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
                name: "UserReferrals",
                schema: "mlm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Path = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    MaterializedPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReferralCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeactivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserReferrals", x => x.Id);
                    table.UniqueConstraint("AK_UserReferrals_UserId", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserReferrals_UserReferrals_ParentUserId",
                        column: x => x.ParentUserId,
                        principalSchema: "mlm",
                        principalTable: "UserReferrals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommissionLevels",
                schema: "mlm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommissionRuleSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    FixedAmount = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    MaxAmount = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    MinAmount = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommissionLevels_CommissionRuleSets_CommissionRuleSetId",
                        column: x => x.CommissionRuleSetId,
                        principalSchema: "mlm",
                        principalTable: "CommissionRuleSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommissionRuleVersions",
                schema: "mlm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    SchemaJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionRuleVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommissionRuleVersions_CommissionRuleSets_RuleSetId",
                        column: x => x.RuleSetId,
                        principalSchema: "mlm",
                        principalTable: "CommissionRuleSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductRuleBindings",
                schema: "mlm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommissionRuleSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductRuleBindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductRuleBindings_CommissionRuleSets_CommissionRuleSetId",
                        column: x => x.CommissionRuleSetId,
                        principalSchema: "mlm",
                        principalTable: "CommissionRuleSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommissionTransactions",
                schema: "mlm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourcePaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CommissionRuleSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleSetCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RuleVersion = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    SourceAmount = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsSettled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    WalletTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovalNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RejectedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RejectionNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommissionTransactions_CommissionRuleSets_CommissionRuleSetId",
                        column: x => x.CommissionRuleSetId,
                        principalSchema: "mlm",
                        principalTable: "CommissionRuleSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommissionTransactions_UserReferrals_UserId",
                        column: x => x.UserId,
                        principalSchema: "mlm",
                        principalTable: "UserReferrals",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionLevels_IsActive",
                schema: "mlm",
                table: "CommissionLevels",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionLevels_RuleSetId_Level",
                schema: "mlm",
                table: "CommissionLevels",
                columns: new[] { "CommissionRuleSetId", "Level" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRuleSets_Code",
                schema: "mlm",
                table: "CommissionRuleSets",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRuleSets_IsActive",
                schema: "mlm",
                table: "CommissionRuleSets",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRuleSets_IsDefault",
                schema: "mlm",
                table: "CommissionRuleSets",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRuleVersions_IsPublished",
                schema: "mlm",
                table: "CommissionRuleVersions",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRuleVersions_PublishedAt",
                schema: "mlm",
                table: "CommissionRuleVersions",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRuleVersions_RuleSetId_Version",
                schema: "mlm",
                table: "CommissionRuleVersions",
                columns: new[] { "RuleSetId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommissionTransaction_CreatedOn",
                schema: "mlm",
                table: "CommissionTransactions",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionTransaction_SourcePaymentId",
                schema: "mlm",
                table: "CommissionTransactions",
                column: "SourcePaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionTransaction_SourcePaymentId_UserId",
                schema: "mlm",
                table: "CommissionTransactions",
                columns: new[] { "SourcePaymentId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionTransaction_Status",
                schema: "mlm",
                table: "CommissionTransactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionTransaction_Status_CreatedOn",
                schema: "mlm",
                table: "CommissionTransactions",
                columns: new[] { "Status", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionTransaction_UserId",
                schema: "mlm",
                table: "CommissionTransactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionTransaction_UserId_Status",
                schema: "mlm",
                table: "CommissionTransactions",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionTransactions_CommissionRuleSetId",
                schema: "mlm",
                table: "CommissionTransactions",
                column: "CommissionRuleSetId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_OccurredOn",
                schema: "mlm",
                table: "OutboxMessages",
                column: "OccurredOn");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Processing",
                schema: "mlm",
                table: "OutboxMessages",
                columns: new[] { "ProcessedOn", "NextRetryAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Type",
                schema: "mlm",
                table: "OutboxMessages",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ProductRuleBindings_CommissionRuleSetId",
                schema: "mlm",
                table: "ProductRuleBindings",
                column: "CommissionRuleSetId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductRuleBindings_IsActive",
                schema: "mlm",
                table: "ProductRuleBindings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ProductRuleBindings_ProductId",
                schema: "mlm",
                table: "ProductRuleBindings",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductRuleBindings_ProductId_RuleSetId",
                schema: "mlm",
                table: "ProductRuleBindings",
                columns: new[] { "ProductId", "CommissionRuleSetId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductRuleBindings_ValidPeriod",
                schema: "mlm",
                table: "ProductRuleBindings",
                columns: new[] { "ValidFrom", "ValidTo" });

            migrationBuilder.CreateIndex(
                name: "IX_UserReferrals_Level_IsActive",
                schema: "mlm",
                table: "UserReferrals",
                columns: new[] { "Level", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_UserReferrals_ParentUserId",
                schema: "mlm",
                table: "UserReferrals",
                column: "ParentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReferrals_Path",
                schema: "mlm",
                table: "UserReferrals",
                column: "Path");

            migrationBuilder.CreateIndex(
                name: "IX_UserReferrals_UserId",
                schema: "mlm",
                table: "UserReferrals",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommissionLevels",
                schema: "mlm");

            migrationBuilder.DropTable(
                name: "CommissionRuleVersions",
                schema: "mlm");

            migrationBuilder.DropTable(
                name: "CommissionTransactions",
                schema: "mlm");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "mlm");

            migrationBuilder.DropTable(
                name: "ProductRuleBindings",
                schema: "mlm");

            migrationBuilder.DropTable(
                name: "UserReferrals",
                schema: "mlm");

            migrationBuilder.DropTable(
                name: "CommissionRuleSets",
                schema: "mlm");
        }
    }
}
