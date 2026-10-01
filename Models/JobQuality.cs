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
    private static readonly string[] GenericGuidancePhrases =
    [
        "candidates are advised to read the official notification",
        "eligible candidates can apply online through the official website",
        "please read the official notification carefully",
        "make sure you meet the eligibility criteria before applying",
        "apply through the link provided above",
        "for more details visit the official website",
        "keep checking the official website for updates"
    ];
    private static readonly string[] MultiJobPhrases =
    ["multiple companies", "various companies", "top companies hiring", "multiple openings across", "job fair for multiple", "recruitment course", "job placement course", "online job course", "career coaching course"];

    public static DateTime VerificationCutoff(DateTime? utcNow = null) =>
        (utcNow ?? DateTime.UtcNow).AddDays(-VerificationMaxAgeDays);

    public static Expression<Func<Job, bool>> ReadyForIndex(DateTime verifiedAfterUtc)
    {
        Expression<Func<Job, bool>> complete = j =>
        j.Slug != null && j.Slug.Trim() != "" && j.Title.Trim().Length >= 5 && j.CompanyName.Trim().Length >= 2 && j.Role.Trim().Length >= 3 && j.Location.Trim().Length >= 2 &&
        j.Qualification.Trim().Length >= 10 && j.Skills.Trim().Length >= MinimumSkillsLength &&
        j.Description.Trim().Length >= MinimumDescriptionLength &&
        j.SelectionProcess != null && j.SelectionProcess.Trim().Length >= MinimumSelectionProcessLength &&
        j.ApplicationInstructions != null && j.ApplicationInstructions.Trim().Length >= MinimumApplicationInstructionsLength &&
        j.EditorialNote != null && j.EditorialNote.Trim().Length >= MinimumEditorialNoteLength &&
        ((j.ApplyLink.StartsWith("https://") && j.ApplyLink.Length > 8) || (j.ApplyLink.StartsWith("http://") && j.ApplyLink.Length > 7)) && !j.ApplyLink.Contains(" ") &&
        j.OfficialSourceUrl != null && ((j.OfficialSourceUrl.StartsWith("https://") && j.OfficialSourceUrl.Length > 8) || (j.OfficialSourceUrl.StartsWith("http://") && j.OfficialSourceUrl.Length > 7)) && !j.OfficialSourceUrl.Contains(" ") &&
        j.Eligibility != null && j.Eligibility.Trim().Length >= MinimumEligibilityLength &&
        JobCategories.All.Contains(j.Category) && JobSourceTypes.All.Contains(j.SourceType ?? "") &&
        !j.ApplyLink.Contains("@") && j.OfficialSourceUrl != null && !j.OfficialSourceUrl.Contains("@") &&
        !(j.WalkInStartDate.HasValue && j.WalkInEndDate.HasValue && j.WalkInEndDate < j.WalkInStartDate) &&
        !(j.ApplicationInstructions!.ToLower().Contains("candidates are advised to read the official notification") ||
          j.ApplicationInstructions.ToLower().Contains("eligible candidates can apply online through the official website") ||
          j.ApplicationInstructions.ToLower().Contains("please read the official notification carefully") ||
          j.ApplicationInstructions.ToLower().Contains("make sure you meet the eligibility criteria before applying") ||
          j.ApplicationInstructions.ToLower().Contains("apply through the link provided above") ||
          j.ApplicationInstructions.ToLower().Contains("for more details visit the official website") ||
          j.ApplicationInstructions.ToLower().Contains("keep checking the official website for updates") ||
          j.EditorialNote!.ToLower().Contains("candidates are advised to read the official notification") ||
          j.EditorialNote.ToLower().Contains("eligible candidates can apply online through the official website") ||
          j.EditorialNote.ToLower().Contains("please read the official notification carefully") ||
          j.EditorialNote.ToLower().Contains("make sure you meet the eligibility criteria before applying") ||
          j.EditorialNote.ToLower().Contains("apply through the link provided above") ||
          j.EditorialNote.ToLower().Contains("for more details visit the official website") ||
          j.EditorialNote.ToLower().Contains("keep checking the official website for updates")) &&
        !(j.Title.ToLower().Contains("multiple companies") || j.Title.ToLower().Contains("various companies") ||
          j.Title.ToLower().Contains("top 10") || j.Title.ToLower().Contains("best companies") ||
          j.Title.ToLower().Contains("job fair") || j.Title.ToLower().Contains("job placement course") || j.Title.ToLower().Contains("recruitment course") ||
          j.Title.ToLower().Contains("top companies hiring") || j.Title.ToLower().Contains("top 5") || j.Title.ToLower().Contains("top 20") ||
          j.Title.ToLower().Contains("online job course") || j.Title.ToLower().Contains("career coaching course") ||
          j.CompanyName.ToLower().Contains("multiple companies") || j.CompanyName.ToLower().Contains("various companies") ||
          j.CompanyName.ToLower().Contains("multiple employers") || j.CompanyName.ToLower().Contains("various employers") ||
          j.Role.ToLower().Contains("job placement course") || j.Role.ToLower().Contains("career coaching course") ||
          j.Description.ToLower().Contains("multiple openings across companies") || j.Description.ToLower().Contains("course enrollment") ||
          j.Description.ToLower().Contains("course fee") || j.Description.ToLower().Contains("top companies hiring") ||
          j.Description.ToLower().Contains("course for job placement"));
        return complete;
    }

    public static Expression<Func<Job, bool>> NeedsReview(DateTime verifiedAfterUtc) => j =>
        j.Slug == null || j.Slug.Trim() == "" || j.Title.Trim().Length < 5 || j.CompanyName.Trim().Length < 2 || j.Role.Trim().Length < 3 || j.Location.Trim().Length < 2 ||
        j.Qualification.Trim().Length < 10 || j.Skills.Trim().Length < MinimumSkillsLength ||
        j.Description.Trim().Length < MinimumDescriptionLength ||
        j.SelectionProcess == null || j.SelectionProcess.Trim().Length < MinimumSelectionProcessLength ||
        j.ApplicationInstructions == null || j.ApplicationInstructions.Trim().Length < MinimumApplicationInstructionsLength ||
        j.EditorialNote == null || j.EditorialNote.Trim().Length < MinimumEditorialNoteLength ||
        ((!j.ApplyLink.StartsWith("https://") || j.ApplyLink.Length <= 8) && (!j.ApplyLink.StartsWith("http://") || j.ApplyLink.Length <= 7)) || j.ApplyLink.Contains(" ") ||
        j.OfficialSourceUrl == null || ((!j.OfficialSourceUrl.StartsWith("https://") || j.OfficialSourceUrl.Length <= 8) && (!j.OfficialSourceUrl.StartsWith("http://") || j.OfficialSourceUrl.Length <= 7)) || j.OfficialSourceUrl.Contains(" ") ||
        j.Eligibility == null || j.Eligibility.Trim().Length < MinimumEligibilityLength ||
        !JobCategories.All.Contains(j.Category) || !JobSourceTypes.All.Contains(j.SourceType ?? "") ||
        j.ApplyLink.Contains("@") || (j.OfficialSourceUrl != null && j.OfficialSourceUrl.Contains("@")) ||
        (j.WalkInStartDate.HasValue && j.WalkInEndDate.HasValue && j.WalkInEndDate < j.WalkInStartDate) ||
        (j.ApplicationInstructions != null && (j.ApplicationInstructions.ToLower().Contains("candidates are advised to read the official notification") ||
          j.ApplicationInstructions.ToLower().Contains("eligible candidates can apply online through the official website") ||
          j.ApplicationInstructions.ToLower().Contains("please read the official notification carefully") ||
          j.ApplicationInstructions.ToLower().Contains("make sure you meet the eligibility criteria before applying") ||
          j.ApplicationInstructions.ToLower().Contains("apply through the link provided above") ||
          j.ApplicationInstructions.ToLower().Contains("for more details visit the official website") ||
          j.ApplicationInstructions.ToLower().Contains("keep checking the official website for updates"))) ||
        (j.EditorialNote != null && (j.EditorialNote.ToLower().Contains("candidates are advised to read the official notification") ||
          j.EditorialNote.ToLower().Contains("eligible candidates can apply online through the official website") ||
          j.EditorialNote.ToLower().Contains("please read the official notification carefully") ||
          j.EditorialNote.ToLower().Contains("make sure you meet the eligibility criteria before applying") ||
          j.EditorialNote.ToLower().Contains("apply through the link provided above") ||
          j.EditorialNote.ToLower().Contains("for more details visit the official website") ||
          j.EditorialNote.ToLower().Contains("keep checking the official website for updates"))) ||
        j.Title.ToLower().Contains("multiple companies") || j.Title.ToLower().Contains("various companies") ||
        j.Title.ToLower().Contains("top 10") || j.Title.ToLower().Contains("best companies") || j.Title.ToLower().Contains("job fair") ||
        j.Title.ToLower().Contains("job placement course") || j.Title.ToLower().Contains("recruitment course") || j.Title.ToLower().Contains("top companies hiring") ||
        j.Title.ToLower().Contains("top 5") || j.Title.ToLower().Contains("top 20") || j.Title.ToLower().Contains("online job course") ||
        j.Title.ToLower().Contains("career coaching course") ||
        j.CompanyName.ToLower().Contains("multiple companies") || j.CompanyName.ToLower().Contains("various companies") ||
        j.CompanyName.ToLower().Contains("multiple employers") || j.CompanyName.ToLower().Contains("various employers") ||
        j.Role.ToLower().Contains("job placement course") || j.Role.ToLower().Contains("career coaching course") ||
        j.Description.ToLower().Contains("multiple openings across companies") || j.Description.ToLower().Contains("course enrollment") ||
        j.Description.ToLower().Contains("course fee") || j.Description.ToLower().Contains("top companies hiring") ||
        j.Description.ToLower().Contains("course for job placement");

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
        if (!JobCategories.IsSupported(job.Category)) issues.Add("supported job category");
        if (JobSourceTypes.Normalize(job.SourceType) == null) issues.Add("source type (OfficialEmployer, GovernmentPortal, JobPortal, or Other)");
        if (HasGenericGuidance(job.ApplicationInstructions) || HasGenericGuidance(job.EditorialNote)) issues.Add("generic or repeated editorial/application text needs manual review");
        if (HasMultiJobContent(job)) issues.Add("single-job listing (not a multi-company, listicle, or course promotion)");
        if (!ValidHttpUrl(job.ApplyLink)) issues.Add("valid application URL");
        if (!ValidHttpUrl(job.OfficialSourceUrl)) issues.Add("valid official source URL");
        if (job.WalkInStartDate.HasValue && job.WalkInEndDate.HasValue && job.WalkInEndDate < job.WalkInStartDate) issues.Add("valid walk-in date range");
        if (job.SourcePostedDate.HasValue && job.SourcePostedDate.Value.Date > DateTime.UtcNow.Date) issues.Add("source posting date not in the future");
        return issues;
    }

    private static void AddIf(List<string> issues, string? value, int minimumLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length < minimumLength) issues.Add(label);
    }

    private static bool ValidHttpUrl(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        uri.Scheme is "https" or "http" && string.IsNullOrEmpty(uri.UserInfo);

    public static bool HasGenericGuidance(string? value)
    {
        var normalized = NormalizeGuidance(value);
        return normalized.Length > 0 && GenericGuidancePhrases.Any(phrase => normalized.Contains(NormalizeGuidance(phrase), StringComparison.Ordinal));
    }

    public static string NormalizeGuidance(string? value) => string.Join(' ', (value ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim().ToLowerInvariant();

    public static bool HasMultiJobContent(Job job)
    {
        var text = $"{job.Title} {job.CompanyName} {job.Role} {job.Description}".ToLowerInvariant();
        return MultiJobPhrases.Any(text.Contains) ||
            job.Title.Contains("top 10", StringComparison.OrdinalIgnoreCase) ||
            job.Title.Contains("best companies", StringComparison.OrdinalIgnoreCase) ||
            job.Title.Contains("courses for jobs", StringComparison.OrdinalIgnoreCase) ||
            job.Title.Contains("job fair", StringComparison.OrdinalIgnoreCase) ||
            job.Title.Contains("top 5", StringComparison.OrdinalIgnoreCase) ||
            job.Title.Contains("top 20", StringComparison.OrdinalIgnoreCase) ||
            job.CompanyName.Contains("multiple employers", StringComparison.OrdinalIgnoreCase) ||
            job.CompanyName.Contains("various employers", StringComparison.OrdinalIgnoreCase) ||
            job.Description.Contains("multiple openings across companies", StringComparison.OrdinalIgnoreCase) ||
            job.Description.Contains("course enrollment", StringComparison.OrdinalIgnoreCase) ||
            job.Description.Contains("course fee", StringComparison.OrdinalIgnoreCase);
    }

}
