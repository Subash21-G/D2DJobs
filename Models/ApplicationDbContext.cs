using Microsoft.EntityFrameworkCore;
namespace JobForFresher.Models;
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Job> Jobs { get; set; }
    public DbSet<DailyAnalytics> DailyAnalytics { get; set; }
    public DbSet<AdminUser> AdminUsers { get; set; }
    public DbSet<SavedJob> SavedJobs { get; set; }
    public DbSet<Applicant> Applicants { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Job>().HasIndex(j => new { j.IsActive, j.PostedDate });
        modelBuilder.Entity<Job>().HasIndex(j => j.ExpiryDate);
        modelBuilder.Entity<AdminUser>().Property(a => a.SecurityStamp).HasMaxLength(36);
    }
}