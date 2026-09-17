using System.ComponentModel.DataAnnotations;
namespace JobForFresher.Models;
public class SiteSetupInput : IValidatableObject
{
    [Required] public SiteOptions Site { get; set; } = new();
    public bool AdsEnabled { get; set; }
    public bool SiteApproved { get; set; }
    public bool ConsentConfigured { get; set; }
    [RegularExpression(@"^ca-pub-[0-9]{16}$")] public string? PublisherId { get; set; }
    [RegularExpression(@"^[0-9]{10}$")] public string? ListingSlot { get; set; }
    [RegularExpression(@"^[0-9]{10}$")] public string? DetailSlot { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AdsEnabled && (!SiteApproved || !ConsentConfigured || string.IsNullOrWhiteSpace(PublisherId) || (string.IsNullOrWhiteSpace(ListingSlot) && string.IsNullOrWhiteSpace(DetailSlot))))
            yield return new("To enable ads, supply a publisher ID and at least one slot, confirm site approval and complete the consent setup.", [nameof(AdsEnabled)]);
    }
}