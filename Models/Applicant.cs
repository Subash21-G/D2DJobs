namespace JobForFresher.Models;
// Preserve the legacy table and its data; resumes are no longer served publicly.
public class Applicant
{
    public int Id { get; set; }
    public DateTime AppliedDate { get; set; }
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public int JobId { get; set; }
    public string Phone { get; set; } = "";
    public string Resume { get; set; } = "";
}