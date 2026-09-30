using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobForFresher.Migrations
{
    /// <inheritdoc />
    public partial class AddJobEditorialContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationInstructions",
                table: "Jobs",
                type: "nvarchar(max)",
                maxLength: 5000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentsRequired",
                table: "Jobs",
                type: "nvarchar(3000)",
                maxLength: 3000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EditorialNote",
                table: "Jobs",
                type: "nvarchar(3000)",
                maxLength: 3000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicationInstructions",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "DocumentsRequired",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "EditorialNote",
                table: "Jobs");
        }
    }
}
