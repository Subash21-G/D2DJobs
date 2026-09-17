using System;
using JobForFresher.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace JobForFresher.Migrations;
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260917000100_AddDailyAnalytics")]
public class AddDailyAnalytics : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.CreateTable(
        name: "DailyAnalytics",
        columns: table => new {
            Date = table.Column<DateTime>(type: "date", nullable: false),
            PageViews = table.Column<long>(type: "bigint", nullable: false),
            AdImpressions = table.Column<long>(type: "bigint", nullable: true),
            AdClicks = table.Column<long>(type: "bigint", nullable: true),
            ReportUpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
        }, constraints: table => table.PrimaryKey("PK_DailyAnalytics", x => x.Date));
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("DailyAnalytics");
}