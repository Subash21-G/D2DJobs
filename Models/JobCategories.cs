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
    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Freshers"] = "Freshers Jobs", ["Fresher Jobs"] = "Freshers Jobs",
        ["Off Campus Jobs"] = "Off Campus", ["Off Campus Drive"] = "Off Campus", ["Off Campus Drives"] = "Off Campus", ["Off-Campus"] = "Off Campus",
        ["Walk In"] = "Walk-in", ["Walk-in Drive"] = "Walk-in", ["Walk-in Jobs"] = "Walk-in", ["Walk-in Interviews"] = "Walk-in",
        ["IT / Software Jobs"] = "IT Jobs", ["Software Jobs"] = "IT Jobs",
        ["Government / PSU Jobs"] = "Government Jobs", ["Govt Jobs"] = "Government Jobs",
        ["Banking Jobs"] = "Bank Jobs", ["Internships"] = "Internship Programs", ["Internship"] = "Internship Programs",
        ["WFH Jobs"] = "Work From Home Jobs", ["Remote Jobs"] = "Work From Home Jobs",
        ["Core Jobs"] = "Core Engineering Jobs", ["Engineering Jobs"] = "Core Engineering Jobs",
        ["BPO Jobs"] = "BPO / Support Jobs", ["Support Jobs"] = "BPO / Support Jobs"
    };
    public static string? Normalize(string? category)
    {
        var value = category?.Trim();
        if (string.IsNullOrEmpty(value)) return null;
        if (Routes.Keys.FirstOrDefault(name => name.Equals(value, StringComparison.OrdinalIgnoreCase)) is { } canonical) return canonical;
        return Aliases.TryGetValue(value, out var mapped) ? mapped : null;
    }
    public static bool IsSupported(string? category) => category != null && All.Contains(category, StringComparer.OrdinalIgnoreCase);
}
