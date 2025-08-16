using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAxis.Modules.ApiManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ApiManager");

            migrationBuilder.CreateTable(
                name: "SecurityProfiles",
                schema: "ApiManager",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    ConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RotationPolicy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebServices",
                schema: "ApiManager",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BaseUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SecurityProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    OwnerTenantId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebServices_SecurityProfiles_SecurityProfileId",
                        column: x => x.SecurityProfileId,
                        principalSchema: "ApiManager",
                        principalTable: "SecurityProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WebServiceMethods",
                schema: "ApiManager",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WebServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RequestSchema = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseSchema = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeoutMs = table.Column<int>(type: "int", nullable: false),
                    RetryPolicyJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CircuitPolicyJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebServiceMethods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebServiceMethods_WebServices_WebServiceId",
                        column: x => x.WebServiceId,
                        principalSchema: "ApiManager",
                        principalTable: "WebServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WebServiceCallLogs",
                schema: "ApiManager",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WebServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MethodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RequestDump = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseDump = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusCode = table.Column<int>(type: "int", nullable: true),
                    LatencyMs = table.Column<long>(type: "bigint", nullable: false),
                    Succeeded = table.Column<bool>(type: "bit", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebServiceCallLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebServiceCallLogs_WebServiceMethods_MethodId",
                        column: x => x.MethodId,
                        principalSchema: "ApiManager",
                        principalTable: "WebServiceMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WebServiceCallLogs_WebServices_WebServiceId",
                        column: x => x.WebServiceId,
                        principalSchema: "ApiManager",
                        principalTable: "WebServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WebServiceParams",
                schema: "ApiManager",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MethodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Location = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    DefaultValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebServiceParams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebServiceParams_WebServiceMethods_MethodId",
                        column: x => x.MethodId,
                        principalSchema: "ApiManager",
                        principalTable: "WebServiceMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityProfiles_Type",
                schema: "ApiManager",
                table: "SecurityProfiles",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_WebServiceCallLogs_CorrelationId",
                schema: "ApiManager",
                table: "WebServiceCallLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_WebServiceCallLogs_MethodId_CreatedAt",
                schema: "ApiManager",
                table: "WebServiceCallLogs",
                columns: new[] { "MethodId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WebServiceCallLogs_Succeeded_CreatedAt",
                schema: "ApiManager",
                table: "WebServiceCallLogs",
                columns: new[] { "Succeeded", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WebServiceCallLogs_WebServiceId",
                schema: "ApiManager",
                table: "WebServiceCallLogs",
                column: "WebServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_WebServiceMethods_IsActive",
                schema: "ApiManager",
                table: "WebServiceMethods",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_WebServiceMethods_WebServiceId",
                schema: "ApiManager",
                table: "WebServiceMethods",
                column: "WebServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_WebServiceMethods_WebServiceId_Path_HttpMethod",
                schema: "ApiManager",
                table: "WebServiceMethods",
                columns: new[] { "WebServiceId", "Path", "HttpMethod" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebServiceParams_MethodId",
                schema: "ApiManager",
                table: "WebServiceParams",
                column: "MethodId");

            migrationBuilder.CreateIndex(
                name: "IX_WebServiceParams_MethodId_Name",
                schema: "ApiManager",
                table: "WebServiceParams",
                columns: new[] { "MethodId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebServices_IsActive",
                schema: "ApiManager",
                table: "WebServices",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_WebServices_Name",
                schema: "ApiManager",
                table: "WebServices",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_WebServices_OwnerTenantId",
                schema: "ApiManager",
                table: "WebServices",
                column: "OwnerTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WebServices_SecurityProfileId",
                schema: "ApiManager",
                table: "WebServices",
                column: "SecurityProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WebServiceCallLogs",
                schema: "ApiManager");

            migrationBuilder.DropTable(
                name: "WebServiceParams",
                schema: "ApiManager");

            migrationBuilder.DropTable(
                name: "WebServiceMethods",
                schema: "ApiManager");

            migrationBuilder.DropTable(
                name: "WebServices",
                schema: "ApiManager");

            migrationBuilder.DropTable(
                name: "SecurityProfiles",
                schema: "ApiManager");
        }
    }
}
