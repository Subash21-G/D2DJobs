using System.Linq.Expressions;
namespace JobForFresher.Models;
public static class JobQuality
{
    public const int VerificationMaxAgeDays = 45;
    public const int MinimumDescriptionLength = 200;
    public const int MinimumEligibilityLength = 50;
    public const int MinimumSelectionProcessLength = 30;
    public const int MinimumSkillsLength = 20;
    public const int MinimumApplicationInstructionsLength = 80;
    public const int MinimumEditorialNoteLength = 80;

    public static DateTime VerificationCutoff(DateTime? utcNow = null) =>
        (utcNow ?? DateTime.UtcNow).AddDays(-VerificationMaxAgeDays);

    public static Expression<Func<Job, bool>> ReadyForIndex(DateTime verifiedAfterUtc) => j =>
        j.Slug != null && j.Slug.Trim() != "" && j.Title.Trim().Length >= 5 && j.CompanyName.Trim().Length >= 2 && j.Role.Trim().Length >= 3 && j.Location.Trim().Length >= 2 &&
        j.Qualification.Trim().Length >= 10 && j.Skills.Trim().Length >= MinimumSkillsLength &&
        j.Description.Trim().Length >= MinimumDescriptionLength &&
        j.SelectionProcess != null && j.SelectionProcess.Trim().Length >= MinimumSelectionProcessLength &&
        j.ApplicationInstructions != null && j.ApplicationInstructions.Trim().Length >= MinimumApplicationInstructionsLength &&
        j.EditorialNote != null && j.EditorialNote.Trim().Length >= MinimumEditorialNoteLength &&
        (j.ApplyLink.StartsWith("https://") || j.ApplyLink.StartsWith("http://")) &&
        j.OfficialSourceUrl != null && (j.OfficialSourceUrl.StartsWith("https://") || j.OfficialSourceUrl.StartsWith("http://")) &&
        j.Eligibility != null && j.Eligibility.Trim().Length >= MinimumEligibilityLength &&
        j.LastVerifiedUtc != null && j.LastVerifiedUtc >= verifiedAfterUtc;

    public static Expression<Func<Job, bool>> NeedsReview(DateTime verifiedAfterUtc) => j =>
        j.Slug == null || j.Slug.Trim() == "" || j.Title.Trim().Length < 5 || j.CompanyName.Trim().Length < 2 || j.Role.Trim().Length < 3 || j.Location.Trim().Length < 2 ||
        j.Qualification.Trim().Length < 10 || j.Skills.Trim().Length < MinimumSkillsLength ||
        j.Description.Trim().Length < MinimumDescriptionLength ||
        j.SelectionProcess == null || j.SelectionProcess.Trim().Length < MinimumSelectionProcessLength ||
        j.ApplicationInstructions == null || j.ApplicationInstructions.Trim().Length < MinimumApplicationInstructionsLength ||
        j.EditorialNote == null || j.EditorialNote.Trim().Length < MinimumEditorialNoteLength ||
        (!j.ApplyLink.StartsWith("https://") && !j.ApplyLink.StartsWith("http://")) ||
        j.OfficialSourceUrl == null || (!j.OfficialSourceUrl.StartsWith("https://") && !j.OfficialSourceUrl.StartsWith("http://")) ||
        j.Eligibility == null || j.Eligibility.Trim().Length < MinimumEligibilityLength ||
        j.LastVerifiedUtc == null || j.LastVerifiedUtc < verifiedAfterUtc;

    public static bool RequiresReview(Job job, DateTime? utcNow = null) => ReviewIssues(job, utcNow).Count > 0;

    public static IReadOnlyList<string> ReviewIssues(Job job, DateTime? utcNow = null)
    {
        var issues = new List<string>();
        AddIf(issues, job.Slug, 1, "public job URL");
        AddIf(issues, job.Title, 5, "job title");
        AddIf(issues, job.CompanyName, 2, "company name");
        AddIf(issues, job.Role, 3, "role");
        AddIf(issues, job.Location, 2, "location");
        AddIf(issues, job.Qualification, 10, "qualification details");
        AddIf(issues, job.Skills, MinimumSkillsLength, "skills and requirements");
        AddIf(issues, job.Description, MinimumDescriptionLength, "substantial job description");
        AddIf(issues, job.Eligibility, MinimumEligibilityLength, "detailed eligibility");
        AddIf(issues, job.SelectionProcess, MinimumSelectionProcessLength, "selection process");
        AddIf(issues, job.ApplicationInstructions, MinimumApplicationInstructionsLength, "job-specific application instructions");
        AddIf(issues, job.EditorialNote, MinimumEditorialNoteLength, "D2DJobs editorial guidance");
        if (!ValidHttpUrl(job.ApplyLink)) issues.Add("valid application URL");
        if (!ValidHttpUrl(job.OfficialSourceUrl)) issues.Add("valid official source URL");
        if (!job.LastVerifiedUtc.HasValue) issues.Add("last verified date");
        else if (job.LastVerifiedUtc < VerificationCutoff(utcNow)) issues.Add($"verification within the last {VerificationMaxAgeDays} days");
        return issues;
    }

    private static void AddIf(List<string> issues, string? value, int minimumLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length < minimumLength) issues.Add(label);
    }

    private static bool ValidHttpUrl(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        uri.Scheme is "https" or "http" && string.IsNullOrEmpty(uri.UserInfo);
}
