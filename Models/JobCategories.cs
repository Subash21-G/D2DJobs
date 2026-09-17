namespace JobForFresher.Models;
public static class JobCategories
{
    public static readonly string[] All = ["Freshers Jobs", "Off Campus", "Walk-in", "IT Jobs", "Government Jobs", "Bank Jobs", "Internship Programs", "Work From Home Jobs", "Core Engineering Jobs", "BPO / Support Jobs"];
    private static readonly Dictionary<string,string> Routes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Freshers Jobs"]="freshers", ["Off Campus"]="off-campus", ["Walk-in"]="walk-in", ["IT Jobs"]="it-software", ["Government Jobs"]="government", ["Bank Jobs"]="bank", ["Internship Programs"]="internships", ["Work From Home Jobs"]="work-from-home", ["Core Engineering Jobs"]="core-engineering", ["BPO / Support Jobs"]="bpo-support"
    };
    public static string? Slug(string? category) => category != null && Routes.TryGetValue(category,out var slug) ? slug : null;
    public static string? Name(string slug) => Routes.FirstOrDefault(pair => pair.Value.Equals(slug,StringComparison.OrdinalIgnoreCase)).Key;
}