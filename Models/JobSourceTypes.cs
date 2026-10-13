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

    public static string DetectFromOfficialSourceUrl(string? sourceUrl, string? companyName)
    {
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(uri.UserInfo)) return Other;

        var host = uri.IdnHost.TrimEnd('.').ToLowerInvariant();
        if (host.EndsWith(".gov.in", StringComparison.Ordinal) || host.EndsWith(".nic.in", StringComparison.Ordinal) ||
            host.Equals("gov.in", StringComparison.Ordinal) || host.EndsWith(".gov", StringComparison.Ordinal) ||
            host.EndsWith(".gov.uk", StringComparison.Ordinal) || host.EndsWith(".gov.au", StringComparison.Ordinal))
            return GovernmentPortal;

        string[] jobBoardDomains =
        [
            "naukri.com", "linkedin.com", "indeed.com", "glassdoor.com", "foundit.in", "shine.com",
            "timesjobs.com", "freshersworld.com", "internshala.com", "apna.co", "workindia.in",
            "cutshort.io", "hirist.com", "wellfound.com", "monsterindia.com", "ziprecruiter.com",
            "simplyhired.com"
        ];
        if (jobBoardDomains.Any(domain => host == domain || host.EndsWith("." + domain, StringComparison.Ordinal)))
            return JobPortal;

        var companyTokens = System.Text.RegularExpressions.Regex.Split(companyName?.ToLowerInvariant() ?? "", "[^a-z0-9]+")
            .Where(token => token.Length >= 5 && token is not ("private" or "limited" or "technologies" or "technology" or "solutions" or "services" or "company" or "group" or "india" or "international"))
            .ToArray();
        if (companyTokens.Any(token => host.Contains(token, StringComparison.Ordinal)))
            return OfficialEmployer;

        return Other;
    }

    public static string Resolve(string? sourceType, string? sourceUrl, string? companyName)
    {
        var normalized = Normalize(sourceType);
        if (normalized is not null && normalized != Other) return normalized;
        var detected = DetectFromOfficialSourceUrl(sourceUrl, companyName);
        return detected == Other ? normalized ?? Other : detected;
    }
}
