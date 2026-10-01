using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobForFresher.Migrations
{
    /// <inheritdoc />
    public partial class ActivateImportedJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [Jobs] SET [IsActive] = 1 WHERE [ImportedUtc] IS NOT NULL AND [IsActive] = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The previous active state cannot be reconstructed safely.
        }
    }
}
