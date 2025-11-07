using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAxis.Modules.ProductOrderModule.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Count",
                schema: "productorder",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Count",
                schema: "productorder",
                table: "Products");
        }
    }
}
