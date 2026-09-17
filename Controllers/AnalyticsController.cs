using JobForFresher.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace JobForFresher.Controllers;
[Microsoft.AspNetCore.Authorization.Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AnalyticsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(int days = 30)
    {

        days = days is 7 or 90 ? days : 30;
        var today = DateTime.UtcNow.Date;
        ViewBag.Days = days;
        ViewBag.JobViews = await db.Jobs.SumAsync(j => (long)j.ViewsCount);
        ViewBag.ApplyClicks = await db.Jobs.SumAsync(j => (long)j.ApplyClicks);
        var rows = await db.DailyAnalytics.AsNoTracking().Where(r => r.Date >= today.AddDays(1-days) && r.Date <= today).OrderByDescending(r => r.Date).ToListAsync();
        return View(rows);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveReport(AdReportInput input)
    {

        if (!ModelState.IsValid || input.Date > DateTime.UtcNow.Date || input.Date < new DateTime(2000, 1, 1))
        {
            TempData["AnalyticsError"] = "Enter a valid date (not in the future) and non-negative whole numbers up to 1 billion for both metrics.";
            return RedirectToAction(nameof(Index));
        }
        var date = input.Date!.Value.Date;
        var updated = DateTime.UtcNow;
        await db.Database.ExecuteSqlInterpolatedAsync($@"
MERGE [DailyAnalytics] WITH (HOLDLOCK) AS target
USING (SELECT {date} AS [Date]) AS source ON target.[Date] = source.[Date]
WHEN MATCHED THEN UPDATE SET AdImpressions={input.Impressions}, AdClicks={input.Clicks}, ReportUpdatedUtc={updated}
WHEN NOT MATCHED THEN INSERT ([Date],PageViews,AdImpressions,AdClicks,ReportUpdatedUtc)
VALUES ({date},0,{input.Impressions},{input.Clicks},{updated});");
        TempData["AnalyticsNotice"] = $"AdSense figures saved for {date:yyyy-MM-dd}. Saving the same date replaces its previous ad figures.";
        return RedirectToAction(nameof(Index));
    }
}