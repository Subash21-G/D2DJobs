using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobForFresher.Migrations
{
    /// <inheritdoc />
    public partial class AddJobSourceMetadataAndWalkInRange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ImportedUtc",
                table: "Jobs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SourcePostedDate",
                table: "Jobs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "Jobs",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WalkInEndDate",
                table: "Jobs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WalkInStartDate",
                table: "Jobs",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImportedUtc",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "SourcePostedDate",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "WalkInEndDate",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "WalkInStartDate",
                table: "Jobs");
        }
    }
}
