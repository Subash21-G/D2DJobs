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
        j.LastVerifiedUtc.HasValue && j.LastVerifiedUtc >= verifiedAfterUtc && j.LastVerifiedUtc <= DateTime.UtcNow &&
        (!j.SourcePostedDate.HasValue || j.SourcePostedDate.Value.Date <= DateTime.UtcNow.Date) &&
        (!j.ExpiryDate.HasValue || j.ExpiryDate.Value.Date >= DateTime.UtcNow.Date) &&
        (j.WalkInEndDate.HasValue ? j.WalkInEndDate.Value.Date >= DateTime.UtcNow.Date : (!j.WalkInDate.HasValue || j.WalkInDate.Value.Date >= DateTime.UtcNow.Date)) &&
        ((j.ApplyLink.StartsWith("https://") && j.ApplyLink.Length > 8) || (j.ApplyLink.StartsWith("http://") && j.ApplyLink.Length > 7)) && !j.ApplyLink.Contains(" ") &&
        (j.ApplyLink.IndexOf("/", j.ApplyLink.IndexOf("://") + 3) < 0 || j.ApplyLink.IndexOf("/", j.ApplyLink.IndexOf("://") + 3) == j.ApplyLink.Length - 1 ||
         (j.ApplyLink.IndexOf("/", j.ApplyLink.IndexOf("://") + 3) > 0 && j.ApplyLink.IndexOf("/", j.ApplyLink.IndexOf("://") + 3) < j.ApplyLink.Length - 1)) &&
        (!j.ApplyLink.EndsWith("/") || j.ApplyLink.Contains("?")) &&
        !(j.ApplyLink.ToLower().EndsWith("/careers") || j.ApplyLink.ToLower().EndsWith("/career") || j.ApplyLink.ToLower().EndsWith("/jobs") || j.ApplyLink.ToLower().EndsWith("/job") || j.ApplyLink.ToLower().EndsWith("/recruitment") || j.ApplyLink.ToLower().EndsWith("/vacancies")) &&
        !((j.ApplyLink.ToLower().Contains("/careers?") || j.ApplyLink.ToLower().Contains("/career?") || j.ApplyLink.ToLower().Contains("/jobs?") || j.ApplyLink.ToLower().Contains("/job?") || j.ApplyLink.ToLower().Contains("/recruitment?") || j.ApplyLink.ToLower().Contains("/vacancies?")) &&
          !(j.ApplyLink.ToLower().Contains("?job") || j.ApplyLink.ToLower().Contains("&job") || j.ApplyLink.ToLower().Contains("vacancy") || j.ApplyLink.ToLower().Contains("requisition") || j.ApplyLink.ToLower().Contains("?req=") || j.ApplyLink.ToLower().Contains("&req=") || j.ApplyLink.ToLower().Contains("?id=") || j.ApplyLink.ToLower().Contains("&id="))) &&
        !((j.ApplyLink.IndexOf("/", j.ApplyLink.IndexOf("://") + 3) < 0 || j.ApplyLink.ToLower().Contains("/?")) &&
          !(j.ApplyLink.ToLower().Contains("?job") || j.ApplyLink.ToLower().Contains("&job") || j.ApplyLink.ToLower().Contains("vacancy") || j.ApplyLink.ToLower().Contains("requisition") || j.ApplyLink.ToLower().Contains("?req=") || j.ApplyLink.ToLower().Contains("&req=") || j.ApplyLink.ToLower().Contains("?id=") || j.ApplyLink.ToLower().Contains("&id="))) &&
        j.OfficialSourceUrl != null && ((j.OfficialSourceUrl.StartsWith("https://") && j.OfficialSourceUrl.Length > 8) || (j.OfficialSourceUrl.StartsWith("http://") && j.OfficialSourceUrl.Length > 7)) && !j.OfficialSourceUrl.Contains(" ") &&
        j.Eligibility != null && j.Eligibility.Trim().Length >= MinimumEligibilityLength &&
        JobCategories.All.Contains(j.Category) && JobSourceTypes.All.Contains(j.SourceType ?? "") && j.SourceType != JobSourceTypes.Other &&
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
        !j.LastVerifiedUtc.HasValue || j.LastVerifiedUtc < verifiedAfterUtc || j.LastVerifiedUtc > DateTime.UtcNow ||
        (j.SourcePostedDate.HasValue && (j.SourcePostedDate.Value.Date > DateTime.UtcNow.Date || j.SourcePostedDate.Value.Date <= DateTime.UtcNow.Date.AddDays(-30))) ||
        (j.ExpiryDate.HasValue && j.ExpiryDate.Value.Date < DateTime.UtcNow.Date) ||
        (j.WalkInEndDate.HasValue ? j.WalkInEndDate.Value.Date < DateTime.UtcNow.Date : (j.WalkInDate.HasValue && j.WalkInDate.Value.Date < DateTime.UtcNow.Date)) ||
        ((!j.ApplyLink.StartsWith("https://") || j.ApplyLink.Length <= 8) && (!j.ApplyLink.StartsWith("http://") || j.ApplyLink.Length <= 7)) || j.ApplyLink.Contains(" ") ||
        ((j.ApplyLink.IndexOf("/", j.ApplyLink.IndexOf("://") + 3) < 0 || j.ApplyLink.ToLower().Contains("/?")) &&
          !(j.ApplyLink.ToLower().Contains("?job") || j.ApplyLink.ToLower().Contains("&job") || j.ApplyLink.ToLower().Contains("vacancy") || j.ApplyLink.ToLower().Contains("requisition") || j.ApplyLink.ToLower().Contains("?req=") || j.ApplyLink.ToLower().Contains("&req=") || j.ApplyLink.ToLower().Contains("?id=") || j.ApplyLink.ToLower().Contains("&id="))) ||
        j.ApplyLink.IndexOf("/", j.ApplyLink.IndexOf("://") + 3) >= j.ApplyLink.Length - 1 ||
        j.OfficialSourceUrl == null || ((!j.OfficialSourceUrl.StartsWith("https://") || j.OfficialSourceUrl.Length <= 8) && (!j.OfficialSourceUrl.StartsWith("http://") || j.OfficialSourceUrl.Length <= 7)) || j.OfficialSourceUrl.Contains(" ") ||
        j.Eligibility == null || j.Eligibility.Trim().Length < MinimumEligibilityLength ||
        !JobCategories.All.Contains(j.Category) || !JobSourceTypes.All.Contains(j.SourceType ?? "") || j.SourceType == JobSourceTypes.Other ||
        j.ApplyLink.Contains("@") || j.ApplyLink.ToLower().EndsWith("/careers") ||
        j.ApplyLink.ToLower().EndsWith("/career") || j.ApplyLink.ToLower().EndsWith("/jobs") ||
        j.ApplyLink.ToLower().EndsWith("/job") || j.ApplyLink.ToLower().EndsWith("/recruitment") ||
        j.ApplyLink.ToLower().EndsWith("/vacancies") ||
        ((j.ApplyLink.ToLower().Contains("/careers?") || j.ApplyLink.ToLower().Contains("/career?") || j.ApplyLink.ToLower().Contains("/jobs?") || j.ApplyLink.ToLower().Contains("/job?") || j.ApplyLink.ToLower().Contains("/recruitment?") || j.ApplyLink.ToLower().Contains("/vacancies?")) &&
          !(j.ApplyLink.ToLower().Contains("?job") || j.ApplyLink.ToLower().Contains("&job") || j.ApplyLink.ToLower().Contains("vacancy") || j.ApplyLink.ToLower().Contains("requisition") || j.ApplyLink.ToLower().Contains("?req=") || j.ApplyLink.ToLower().Contains("&req=") || j.ApplyLink.ToLower().Contains("?id=") || j.ApplyLink.ToLower().Contains("&id="))) ||
        (j.ApplyLink.IndexOf("/", j.ApplyLink.IndexOf("://") + 3) < 0 && !j.ApplyLink.Contains("?")) ||
        (j.ApplyLink.EndsWith("/") && !j.ApplyLink.Contains("?")) ||
        (j.OfficialSourceUrl != null && j.OfficialSourceUrl.Contains("@")) ||
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
        (j.Qualification.ToLower().Contains("verify the full qualification requirements") || j.Qualification.ToLower() == "not specified") ||
        (j.ApplicationInstructions != null &&
            (j.ApplicationInstructions.ToLower().Contains("open the applylink") ||
             j.ApplicationInstructions.ToLower().Contains("officialsourceurl"))) ||
        (j.EditorialNote != null && j.EditorialNote.ToLower().Contains("d2djobs editorial note for")) ||
        j.Description.ToLower().Contains("related role context: professionals in this type of position commonly") ||
        j.Title.ToLower().Contains("roles open") ||
        j.Title.ToLower().Contains("multiple engineering roles") ||
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

    public static bool ContainsInternalApplicationFields(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        (value.Contains("ApplyLink", StringComparison.OrdinalIgnoreCase) ||
         value.Contains("OfficialSourceUrl", StringComparison.OrdinalIgnoreCase));

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
        if ((job.Qualification ?? string.Empty).Contains("verify the full qualification requirements", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(job.Qualification, "Not Specified", StringComparison.OrdinalIgnoreCase))
            issues.Add("replace generic qualification placeholder with source-verified eligibility or an honest unknown status");
        if (ContainsInternalApplicationFields(job.ApplicationInstructions))
            issues.Add("replace ApplyLink/OfficialSourceUrl field names with real candidate-facing application instructions");
        if ((job.EditorialNote ?? "").Contains("D2DJobs editorial note for", StringComparison.OrdinalIgnoreCase))
            issues.Add("replace templated editorial note with genuinely useful, original vacancy-specific guidance");
        if ((job.Description ?? string.Empty).Contains("related role context: professionals in this type of position commonly", StringComparison.OrdinalIgnoreCase))
            issues.Add("verify responsibilities and remove generic boilerplate copied across job pages");
        if (job.Title.Contains("roles open", StringComparison.OrdinalIgnoreCase))
            issues.Add("review multi-role advert and create individually verified vacancy pages where appropriate");
        if (!JobCategories.IsSupported(job.Category)) issues.Add("supported job category");
        if (JobSourceTypes.Normalize(job.SourceType) == null) issues.Add("source type (OfficialEmployer, GovernmentPortal, JobPortal, or Other)");
        else if (JobSourceTypes.Normalize(job.SourceType) == JobSourceTypes.Other) issues.Add("a source type with a clear employer or recruitment authority");
        if (HasGenericGuidance(job.ApplicationInstructions) || HasGenericGuidance(job.EditorialNote)) issues.Add("generic or repeated editorial/application text needs manual review");
        if (HasMultiJobContent(job)) issues.Add("single-job listing (not a multi-company, listicle, or course promotion)");
        if (!ValidHttpUrl(job.ApplyLink)) issues.Add("valid application URL");
        else if (IsGenericApplicationUrl(job.ApplyLink)) issues.Add("vacancy-specific application URL (the employer homepage is not enough)");
        if (!ValidHttpUrl(job.OfficialSourceUrl)) issues.Add("valid official source URL");
        if (!job.LastVerifiedUtc.HasValue) issues.Add("source checked by a person and verification date recorded");
        else if (job.LastVerifiedUtc.Value < VerificationCutoff(utcNow)) issues.Add("source rechecked within the last 45 days");
        if (job.LastVerifiedUtc.HasValue && job.LastVerifiedUtc.Value > (utcNow ?? DateTime.UtcNow)) issues.Add("source verification date not in the future");
        if (job.WalkInStartDate.HasValue && job.WalkInEndDate.HasValue && job.WalkInEndDate < job.WalkInStartDate) issues.Add("valid walk-in date range");
        if (job.SourcePostedDate.HasValue && job.SourcePostedDate.Value.Date > DateTime.UtcNow.Date) issues.Add("source posting date not in the future");
        if (job.SourcePostedDate.HasValue && job.SourcePostedDate.Value.Date <= (utcNow ?? DateTime.UtcNow).Date.AddDays(-30)) issues.Add("recruitment notice is older than 30 days");
        if (job.ExpiryDate.HasValue && job.ExpiryDate.Value.Date < (utcNow ?? DateTime.UtcNow).Date) issues.Add("recruitment closing date has passed");
        var walkInEnd = job.WalkInEndDate ?? job.WalkInDate;
        if (walkInEnd.HasValue && walkInEnd.Value.Date < (utcNow ?? DateTime.UtcNow).Date) issues.Add("walk-in recruitment date has passed");
        if (!walkInEnd.HasValue && job.IsExpired((utcNow ?? DateTime.UtcNow).Date) && !job.ExpiryDate.HasValue && !job.SourcePostedDate.HasValue)
            issues.Add("listing is older than 30 days; recheck the source or close the listing");
        return issues;
    }

    private static void AddIf(List<string> issues, string? value, int minimumLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length < minimumLength) issues.Add(label);
    }

    private static bool ValidHttpUrl(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        uri.Scheme is "https" or "http" && string.IsNullOrEmpty(uri.UserInfo);

    public static bool IsGenericApplicationUrl(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http")) return false;
        var path = uri.AbsolutePath.TrimEnd('/');
        var hasVacancyQuery = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries).Any(pair =>
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0].Replace('+', ' '));
            var queryValue = parts.Length == 2 ? Uri.UnescapeDataString(parts[1].Replace('+', ' ')) : "";
            return queryValue.Length > 0 && (key.Contains("job", StringComparison.OrdinalIgnoreCase) || key.Contains("vacancy", StringComparison.OrdinalIgnoreCase) || key.Contains("requisition", StringComparison.OrdinalIgnoreCase) || key.Equals("req", StringComparison.OrdinalIgnoreCase) || key.Equals("positionid", StringComparison.OrdinalIgnoreCase) || key.Equals("id", StringComparison.OrdinalIgnoreCase));
        });
        if (hasVacancyQuery) return false;
        return path.Length == 0 || path.Equals("/careers", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/career", StringComparison.OrdinalIgnoreCase) || path.Equals("/jobs", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/job", StringComparison.OrdinalIgnoreCase) || path.Equals("/recruitment", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/vacancies", StringComparison.OrdinalIgnoreCase);
    }

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
