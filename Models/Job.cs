using System;
using System.ComponentModel.DataAnnotations;

namespace JobForFresher.Models
{
    public class Job : IValidatableObject
    {
        [Range(1990, 2100)] public int? BatchFrom { get; set; }
        [Range(1990, 2100)] public int? BatchTo { get; set; }
        [StringLength(1000)] public string? OfficialSourceUrl { get; set; }
        [StringLength(5000)] public string? Eligibility { get; set; }
        [StringLength(5000)] public string? SelectionProcess { get; set; }
        [StringLength(5000)] public string? ApplicationInstructions { get; set; }
        [StringLength(3000)] public string? DocumentsRequired { get; set; }
        [StringLength(3000)] public string? EditorialNote { get; set; }
        [StringLength(32)] public string? SourceType { get; set; }
        public DateTime? SourcePostedDate { get; set; }
        public DateTime? ImportedUtc { get; set; }
        public DateTime? WalkInDate { get; set; }
        public DateTime? WalkInStartDate { get; set; }
        public DateTime? WalkInEndDate { get; set; }
        [StringLength(1000)] public string? WalkInVenue { get; set; }
        public DateTime? LastVerifiedUtc { get; set; }
        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            if (BatchFrom.HasValue != BatchTo.HasValue || BatchFrom > BatchTo)
                yield return new("Enter both batch years with the first year no later than the last.", [nameof(BatchFrom), nameof(BatchTo)]);
            foreach (var field in new[] { (nameof(ApplyLink), ApplyLink), (nameof(OfficialSourceUrl), OfficialSourceUrl) })
                if (!string.IsNullOrWhiteSpace(field.Item2) && (!Uri.TryCreate(field.Item2, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http") || uri.UserInfo != ""))
                    yield return new("Use a valid HTTP or HTTPS URL.", [field.Item1]);
            if (LastVerifiedUtc > DateTime.UtcNow) yield return new("The verified date cannot be in the future.", [nameof(LastVerifiedUtc)]);
            if (ExpiryDate.HasValue && AvailableFrom.HasValue && ExpiryDate.Value.Date < AvailableFrom.Value.Date)
                yield return new("The closing date cannot be earlier than the first publication date.", [nameof(ExpiryDate), nameof(AvailableFrom)]);
            if (WalkInStartDate.HasValue && WalkInEndDate.HasValue && WalkInEndDate < WalkInStartDate)
                yield return new("The walk-in end date cannot be earlier than the start date.", [nameof(WalkInStartDate), nameof(WalkInEndDate)]);
        }
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string? CompanyLogo { get; set; }

        public string Category { get; set; } = string.Empty;

        public string SubCategory { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string Experience { get; set; } = string.Empty;

        public string Salary { get; set; } = string.Empty;

        public string JobType { get; set; } = string.Empty;

        public string Qualification { get; set; } = string.Empty;

        public string Skills { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string ApplyLink { get; set; } = string.Empty;

        public bool IsFeatured { get; set; } = false;

        public string? Slug { get; set; }

        public DateTime? ExpiryDate { get; set; }
        public DateTime? AvailableFrom { get; set; }

        public bool IsActive { get; set; } = true;

        public int ViewsCount { get; set; } = 0;

        public int ApplyClicks { get; set; } = 0;

        public DateTime PostedDate { get; set; } = DateTime.Now;
    }
}
