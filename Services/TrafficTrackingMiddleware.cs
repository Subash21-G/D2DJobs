using JobForFresher.Models;
using Microsoft.EntityFrameworkCore;
namespace JobForFresher.Services;
public class TrafficTrackingMiddleware(RequestDelegate next, ILogger<TrafficTrackingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, ApplicationDbContext db)
    {
        var date = DateTime.UtcNow.Date;
        await next(context);
        if (!HttpMethods.IsGet(context.Request.Method) || context.Response.StatusCode != 200 ||
            context.Response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) != true ||
            !string.Equals(context.Request.RouteValues["controller"]?.ToString(), "Home", StringComparison.OrdinalIgnoreCase) ||
            context.User.Identity?.IsAuthenticated == true) return;
        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync($@"
MERGE [DailyAnalytics] WITH (HOLDLOCK) AS target
USING (SELECT {date} AS [Date]) AS source ON target.[Date] = source.[Date]
WHEN MATCHED THEN UPDATE SET PageViews=target.PageViews+1
WHEN NOT MATCHED THEN INSERT ([Date],PageViews) VALUES ({date},1);");
        }
        catch (Exception ex) { logger.LogWarning(ex, "Could not record daily traffic count."); }
    }
}