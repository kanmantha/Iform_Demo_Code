using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IForm.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessoriesFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Family",
                table: "Products",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Material",
                table: "Products",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Family",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Material",
                table: "Products");
        }
    }
}
