using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace JobForFresher.Models;
public class DailyAnalytics
{
    [Key, Column(TypeName = "date")] public DateTime Date { get; set; }
    public long PageViews { get; set; }
    public long? AdImpressions { get; set; }
    public long? AdClicks { get; set; }
    public DateTime? ReportUpdatedUtc { get; set; }
}
public class AdReportInput
{
    [Required, DataType(DataType.Date)] public DateTime? Date { get; set; }
    [Required, Range(0, 1000000000)] public long? Impressions { get; set; }
    [Required, Range(0, 1000000000)] public long? Clicks { get; set; }
}