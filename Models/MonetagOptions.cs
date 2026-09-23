using System.Text.RegularExpressions;
namespace JobForFresher.Models;
public class MonetagOptions
{
    public bool Enabled { get; set; }
    public string ZoneId { get; set; } = "";
    public bool IsReady => Enabled && Regex.IsMatch(ZoneId, "^[0-9]{5,12}$");
}