using System.Linq.Expressions;
namespace JobForFresher.Models;
public static class JobQuality
{
    public static readonly Expression<Func<Job, bool>> NeedsReview = j =>
        j.CompanyName.Trim() == "" || j.Location.Trim() == "" || j.Qualification.Trim() == "" ||
        j.ApplyLink.Trim() == "" || j.Description.Trim() == "" ||
        j.OfficialSourceUrl == null || j.OfficialSourceUrl.Trim() == "" ||
        j.Eligibility == null || j.Eligibility.Trim() == "" || j.LastVerifiedUtc == null;
}
