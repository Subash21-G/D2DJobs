using System.Linq.Expressions;
namespace JobForFresher.Models;
public static class JobQuality
{
    public static readonly Expression<Func<Job, bool>> ReadyForIndex = j =>
        j.Title.Trim() != "" && j.CompanyName.Trim() != "" && j.Role.Trim() != "" && j.Location.Trim() != "" && j.Qualification.Trim() != "" &&
        j.Skills.Trim() != "" && j.Description.Trim() != "" && j.SelectionProcess != null && j.SelectionProcess.Trim() != "" &&
        j.ApplyLink.Trim() != "" && j.OfficialSourceUrl != null && j.OfficialSourceUrl.Trim() != "" &&
        j.Eligibility != null && j.Eligibility.Trim() != "" && j.LastVerifiedUtc != null;

    public static readonly Expression<Func<Job, bool>> NeedsReview = j =>
        j.Title.Trim() == "" || j.CompanyName.Trim() == "" || j.Role.Trim() == "" || j.Location.Trim() == "" || j.Qualification.Trim() == "" ||
        j.Skills.Trim() == "" || j.Description.Trim() == "" || j.SelectionProcess == null || j.SelectionProcess.Trim() == "" ||
        j.ApplyLink.Trim() == "" ||
        j.OfficialSourceUrl == null || j.OfficialSourceUrl.Trim() == "" ||
        j.Eligibility == null || j.Eligibility.Trim() == "" || j.LastVerifiedUtc == null;

    public static bool RequiresReview(Job job) =>
        string.IsNullOrWhiteSpace(job.Title) || string.IsNullOrWhiteSpace(job.CompanyName) ||
        string.IsNullOrWhiteSpace(job.Role) || string.IsNullOrWhiteSpace(job.Location) ||
        string.IsNullOrWhiteSpace(job.Qualification) || string.IsNullOrWhiteSpace(job.Skills) ||
        string.IsNullOrWhiteSpace(job.Description) || string.IsNullOrWhiteSpace(job.SelectionProcess) ||
        string.IsNullOrWhiteSpace(job.ApplyLink) || string.IsNullOrWhiteSpace(job.OfficialSourceUrl) ||
        string.IsNullOrWhiteSpace(job.Eligibility) || job.LastVerifiedUtc == null;
}
