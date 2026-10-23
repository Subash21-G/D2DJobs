using System.Text.RegularExpressions;

namespace JobForFresher.Models;

public sealed class MonetagOptions
{
    public bool Enabled { get; set; }
    public string ZoneId { get; set; } = "";
    public bool IsReady => Enabled && Regex.IsMatch(ZoneId, "^[0-9]{6,12}$");
    public bool SecondaryEnabled { get; set; }
    public string SecondaryZoneId { get; set; } = "";
    public bool SecondaryReady => SecondaryEnabled && Regex.IsMatch(SecondaryZoneId, "^[0-9]{6,12}$");
}
