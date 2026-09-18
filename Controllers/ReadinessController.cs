using JobForFresher.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
namespace JobForFresher.Controllers;

[Authorize, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ReadinessController(IOptionsSnapshot<SiteOptions> site, IOptionsSnapshot<AdvertisingOptions> ads,
    ApplicationDbContext db, ILogger<ReadinessController> logger) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var report = new ReadinessReport();
        report.Checks.Add(new("HTTPS", Request.IsHttps ? "Ready" : "Action needed", Request.IsHttps ? "This request uses HTTPS." : "Open the site over HTTPS and verify hosting configuration."));
        var validation = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var validSite = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(site.Value, new(site.Value), validation, true);
        report.Checks.Add(new("Public domain", validSite && !string.IsNullOrWhiteSpace(site.Value.SiteUrl) ? "Configured" : "Action needed", "Set the public HTTPS origin in Site setup; verify canonical links on the live domain."));
        report.Checks.Add(new("Job alerts", validSite && (!string.IsNullOrWhiteSpace(site.Value.TelegramUrl) || !string.IsNullOrWhiteSpace(site.Value.WhatsAppUrl)) ? "Configured" : "RSS only", "RSS is available. Channel links require your real Telegram or WhatsApp URL; delivery is not automatic."));
        report.Checks.Add(new("Advertising", ads.Value.Slot("ListingInline") != null || ads.Value.Slot("JobSidebar") != null ? "Configured" : "Disabled / incomplete", "Configuration does not verify account approval, consent collection or live ad delivery."));
try
        {
            var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).Count();
            report.Checks.Add(new("Database migrations", pending == 0 ? "Ready" : "Action needed", pending == 0 ? "All migrations in this application are applied." : $"{pending} migration(s) pending. Back up the database and apply the reviewed deployment SQL."));
            if (pending == 0)
            {
                var active = db.Jobs.AsNoTracking().Where(j => j.IsActive && (j.ExpiryDate == null || j.ExpiryDate >= DateTime.Today));
                var total = await active.CountAsync(cancellationToken);
                var incomplete = await active.Where(JobQuality.NeedsReview).CountAsync(cancellationToken);
                report.Checks.Add(new("Active job content", total == 0 || incomplete > 0 ? "Action needed" : "Ready", $"{total} active listing(s); {incomplete} need publishing details. Use the Needs review filter. Values are checked for presence, not factual accuracy."));
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Readiness database check failed.");
            report.Checks.Add(new("Database", "Unverified", "Could not complete the database check. Inspect server logs; connection details are not displayed here."));
        }
        report.Checks.Add(new("Backup and recovery", "Manual verification", "Back up the database, App_Data (including Keys), uploaded logos and deployment secrets. Restore to an isolated environment and verify sign-in and private application data."));
        report.Checks.Add(new("Public hosting", "Manual verification", "Verify HTTPS, private file blocking, populated mobile pages, feeds and application links on the deployed domain."));
        return View(report);
    }
}
