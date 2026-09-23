using System.Text.RegularExpressions;
namespace JobForFresher.Models;
public class MonetagOptions
{
    public bool Enabled { get; set; }
    public string ZoneId { get; set; } = "";
    public string ScriptUrl { get; set; } = "https://auge5.com/88/tag.min.js";
    public bool IsReady => Enabled && Uri.TryCreate(ScriptUrl, UriKind.Absolute, out var uri) && uri.Scheme == "https" && Regex.IsMatch(ZoneId, "^[0-9]{5,12}$");
}