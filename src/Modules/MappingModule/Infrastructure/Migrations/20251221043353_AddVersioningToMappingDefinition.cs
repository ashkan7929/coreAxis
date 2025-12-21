using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAxis.Modules.MappingModule.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVersioningToMappingDefinition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                schema: "mapping",
                table: "MappingDefinitions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Version",
                schema: "mapping",
                table: "MappingDefinitions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Code",
                schema: "mapping",
                table: "MappingDefinitions");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "mapping",
                table: "MappingDefinitions");
        }
    }
}
