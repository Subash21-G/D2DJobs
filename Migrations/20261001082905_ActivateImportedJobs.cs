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
            // Intentionally empty. Imported listings remain inactive until an editor verifies them.
            // Keep this migration ID so EF Core can advance migration history in deployments where it is pending.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: Up performs no schema or data changes to reverse.
        }
    }
}
