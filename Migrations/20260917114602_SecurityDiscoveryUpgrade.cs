using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobForFresher.Migrations
{
    /// <inheritdoc />
    public partial class SecurityDiscoveryUpgrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BatchFrom",
                table: "Jobs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BatchTo",
                table: "Jobs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Eligibility",
                table: "Jobs",
                type: "nvarchar(max)",
                maxLength: 5000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastVerifiedUtc",
                table: "Jobs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficialSourceUrl",
                table: "Jobs",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectionProcess",
                table: "Jobs",
                type: "nvarchar(max)",
                maxLength: 5000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WalkInDate",
                table: "Jobs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WalkInVenue",
                table: "Jobs",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FailedLoginCount",
                table: "AdminUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutUntilUtc",
                table: "AdminUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecurityStamp",
                table: "AdminUsers",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_ExpiryDate",
                table: "Jobs",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_IsActive_PostedDate",
                table: "Jobs",
                columns: new[] { "IsActive", "PostedDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Jobs_ExpiryDate",
                table: "Jobs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_IsActive_PostedDate",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "BatchFrom",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "BatchTo",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Eligibility",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "LastVerifiedUtc",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "OfficialSourceUrl",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "SelectionProcess",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "WalkInDate",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "WalkInVenue",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "FailedLoginCount",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "LockoutUntilUtc",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "AdminUsers");
        }
    }
}
