using Microsoft.EntityFrameworkCore;
namespace JobForFresher.Models;
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Job> Jobs { get; set; }
    public DbSet<DailyAnalytics> DailyAnalytics { get; set; }
    public DbSet<AdminUser> AdminUsers { get; set; }
    public DbSet<SavedJob> SavedJobs { get; set; }
    public DbSet<Applicant> Applicants { get; set; }
    public DbSet<EmployerAccount> EmployerAccounts { get; set; }
    public DbSet<EmployerCampaign> EmployerCampaigns { get; set; }
    public DbSet<CampaignInvoice> CampaignInvoices { get; set; }
    public DbSet<ResumeServiceRequest> ResumeServiceRequests { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Job>().HasIndex(j => new { j.IsActive, j.PostedDate });
        modelBuilder.Entity<Job>().HasIndex(j => j.ExpiryDate);
        modelBuilder.Entity<AdminUser>().Property(a => a.SecurityStamp).HasMaxLength(36);
        modelBuilder.Entity<EmployerAccount>().HasIndex(a => a.NormalizedEmail).IsUnique();
        modelBuilder.Entity<CampaignInvoice>().HasIndex(i => i.Number).IsUnique();
        modelBuilder.Entity<EmployerCampaign>().HasOne(c => c.Job).WithMany().HasForeignKey(c => c.JobId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<EmployerCampaign>().HasIndex(c => c.JobId).IsUnique().HasFilter("[JobId] IS NOT NULL");
    }
}
