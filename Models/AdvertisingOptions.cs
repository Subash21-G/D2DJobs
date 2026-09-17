using System.Text.RegularExpressions;

namespace JobForFresher.Models;

public class AdvertisingOptions
{
    public bool Enabled { get; set; }
    public bool SiteApproved { get; set; }
    public bool ConsentConfigured { get; set; }
    public string PublisherId { get; set; } = "";
    public Dictionary<string, string> Slots { get; set; } = new();
    public bool HasPublisher => Regex.IsMatch(PublisherId, @"^ca-pub-[0-9]{16}$");
    public bool IsReady => Enabled && SiteApproved && ConsentConfigured && HasPublisher;
    public string? Slot(string placement) => IsReady && Slots.TryGetValue(placement, out var slot)
        && Regex.IsMatch(slot, @"^[0-9]{10}$") ? slot : null;
}
