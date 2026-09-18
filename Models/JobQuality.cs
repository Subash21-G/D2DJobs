using System.Linq.Expressions;
namespace JobForFresher.Models;
public static class JobQuality
{
    public static readonly Expression<Func<Job, bool>> NeedsReview = j =>
        j.Title.Trim() == "" || j.CompanyName.Trim() == "" || j.Role.Trim() == "" || j.Location.Trim() == "" || j.Qualification.Trim() == "" ||
        j.Skills.Trim() == "" || j.Description.Trim() == "" || j.SelectionProcess == null || j.SelectionProcess.Trim() == "" ||
        j.ApplyLink.Trim() == "" ||
        j.OfficialSourceUrl == null || j.OfficialSourceUrl.Trim() == "" ||
        j.Eligibility == null || j.Eligibility.Trim() == "" || j.LastVerifiedUtc == null;
}
