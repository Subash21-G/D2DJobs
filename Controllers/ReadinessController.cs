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
        report.Checks.Add(new("AdSense ownership", ads.Value.HasPublisher ? "Configured" : "Action needed", ads.Value.HasPublisher ? "The publisher ID supplies both an ads.txt entry and an ownership meta tag. Confirm AdSense detects the site." : "Add the ca-pub publisher ID so AdSense can verify ownership through ads.txt or the site meta tag."));
        report.Checks.Add(new("Consent platform", ads.Value.ConsentConfigured ? "Verify live" : "Action needed", "Use a Google-certified CMP where required. This setting records your confirmation; it does not install or test the CMP."));
        report.Checks.Add(new("Advertising", ads.Value.Slot("ListingInline") != null || ads.Value.Slot("JobSidebar") != null ? "Configured" : "Disabled / incomplete", "Ad slots activate only after approval, consent confirmation and explicit enablement. Configuration does not verify live ad delivery."));
        report.Checks.Add(new("Human editorial review", "Manual review required", "Before requesting review, have a person compare a varied sample of live listings with their linked employer notices; correct dates, requirements and links, remove expired posts, and check that each page adds useful information beyond the source."));
        report.Checks.Add(new("Originality and audience value", "Manual review required", "Read the homepage, category pages and job details as a first-time visitor. Check whether the site's own reporting and career guidance help readers make decisions, whether repeated text overwhelms job-specific information, and whether anything is auto-generated or copied without human review and added value."));
        report.Checks.Add(new("Paid placement and navigation", "Manual review required", "On desktop and mobile, identify every ad and featured or employer-promoted item. Keep paid placement labelled, ads separate from job actions, and useful publisher content more prominent than advertising."));
try
        {
            var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).Count();
            report.Checks.Add(new("Database migrations", pending == 0 ? "Ready" : "Action needed", pending == 0 ? "All migrations in this application are applied." : $"{pending} migration(s) pending. Back up the database and apply the reviewed deployment SQL."));
            if (pending == 0)
            {
                var active = db.Jobs.AsNoTracking().Where(j => j.IsActive && (j.AvailableFrom == null || j.AvailableFrom <= DateTime.Today) && (j.ExpiryDate == null || j.ExpiryDate >= DateTime.Today));
                var total = await active.CountAsync(cancellationToken);
                var verificationCutoff = JobQuality.VerificationCutoff();
                var verifiedActive = active.Where(JobQuality.ReadyForIndex(verificationCutoff));
                var incomplete = await active.Where(JobQuality.NeedsReview(verificationCutoff)).CountAsync(cancellationToken);
                var duplicateGuidance = await verifiedActive.Where(j => verifiedActive.Any(other => other.Id != j.Id &&
                    (other.ApplicationInstructions!.Trim() == j.ApplicationInstructions!.Trim() ||
                     other.EditorialNote!.Trim() == j.EditorialNote!.Trim()))).CountAsync(cancellationToken);
                incomplete += duplicateGuidance;
                report.Checks.Add(new("Active job content", total == 0 || incomplete > 0 ? "Action needed" : "Ready", $"{total} active listing(s); {incomplete} need substantial publishing details or fresh verification. Use the Needs review filter. Automated checks cannot establish factual accuracy."));
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Readiness database check failed.");
            report.Checks.Add(new("Database", "Unverified", "Could not complete the database check. Inspect server logs; connection details are not displayed here."));
        }
        report.Checks.Add(new("Backup and recovery", "Manual verification", "Back up the database, App_Data (including Keys), uploaded logos and deployment secrets. Restore to an isolated environment and verify sign-in and private application data."));
        report.Checks.Add(new("Public hosting", "Manual verification", "Verify HTTPS, private file blocking, populated mobile pages, feeds and application links on the deployed domain."));
        report.Checks.Add(new("Search Console / sitemap submission", "Recommended", "Submit the production sitemap URL in Google Search Console after the live HTTPS check and confirm that the sitemap can be fetched."));
        return View(report);
    }
}
