namespace JobForFresher.Models;

public static class JobSourceTypes
{
    public const string OfficialEmployer = "OfficialEmployer";
    public const string GovernmentPortal = "GovernmentPortal";
    public const string JobPortal = "JobPortal";
    public const string Other = "Other";
    public static readonly string[] All = [OfficialEmployer, GovernmentPortal, JobPortal, Other];
    public static string? Normalize(string? value) => All.FirstOrDefault(item => item.Equals(value?.Trim(), StringComparison.OrdinalIgnoreCase));
    public static string Label(string? value) => value switch
    {
        OfficialEmployer => "Official employer posting",
        GovernmentPortal => "Official recruitment notice",
        JobPortal => "Job portal listing",
        _ => "Other source"
    };
}
