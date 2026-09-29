using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobForFresher.Models;

public class EmployerAccount
{
    public int Id { get; set; }
    [Required, StringLength(200)] public string CompanyName { get; set; } = "";
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
    [Required, StringLength(254)] public string NormalizedEmail { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    [StringLength(36)] public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();
    public int FailedLoginCount { get; set; }
    public DateTime? LockoutUntilUtc { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public class EmployerCampaign
{
    public int Id { get; set; }
    public int EmployerAccountId { get; set; }
    public EmployerAccount EmployerAccount { get; set; } = null!;
    public int? JobId { get; set; }
    public Job? Job { get; set; }
    [StringLength(200)] public string Title { get; set; } = "";
    [StringLength(100)] public string Category { get; set; } = "";
    [StringLength(200)] public string Location { get; set; } = "";
    [StringLength(200)] public string Qualification { get; set; } = "";
    [StringLength(200)] public string Experience { get; set; } = "";
    [StringLength(200)] public string Salary { get; set; } = "";
    [StringLength(10000)] public string Description { get; set; } = "";
    [StringLength(1000)] public string ApplyLink { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    [StringLength(30)] public string ReviewStatus { get; set; } = "Pending review";
    [StringLength(1000)] public string ReviewNote { get; set; } = "";
    [StringLength(30)] public string UpgradeStatus { get; set; } = "None";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    [Timestamp] public byte[] RowVersion { get; set; } = [];
    public List<CampaignInvoice> Invoices { get; set; } = [];
    [NotMapped] public decimal Ctr => Job?.ViewsCount > 0 ? 100m * Job.ApplyClicks / Job.ViewsCount : 0;
    [NotMapped] public string DisplayStatus => ReviewStatus != "Approved" ? ReviewStatus : Job == null ? "Listing removed" : EndDate.Date < DateTime.Today ? "Ended" : StartDate.Date > DateTime.Today ? "Scheduled" : !Job.IsActive ? "Paused" : "Live";
}

public class CampaignInvoice
{
    public int Id { get; set; }
    public int EmployerCampaignId { get; set; }
    public EmployerCampaign EmployerCampaign { get; set; } = null!;
    [Required, StringLength(80)] public string Number { get; set; } = "";
    [Required, StringLength(500)] public string SellerDetails { get; set; } = "";
    [Required, StringLength(500)] public string BillTo { get; set; } = "";
    [Required, StringLength(300)] public string Description { get; set; } = "";
    [Column(TypeName = "decimal(12,2)")] public decimal Amount { get; set; }
    public DateTime IssuedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PaidUtc { get; set; }
}

public class EmployerRegisterInput
{
    [Required, StringLength(200), Display(Name = "Company name")] public string CompanyName { get; set; } = "";
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 12), DataType(DataType.Password)] public string Password { get; set; } = "";
    [Required, Compare(nameof(Password)), DataType(DataType.Password), Display(Name = "Confirm password")] public string ConfirmPassword { get; set; } = "";
}
public class EmployerLoginInput
{
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
    [Required, StringLength(128), DataType(DataType.Password)] public string Password { get; set; } = "";
}
public class CampaignInput : IValidatableObject
{
    [Required, StringLength(200)] public string Title { get; set; } = "";
    [Required, StringLength(100)] public string Category { get; set; } = "Freshers Jobs";
    [Required, StringLength(200)] public string Location { get; set; } = "";
    [Required, StringLength(200)] public string Qualification { get; set; } = "";
    [Required, StringLength(200)] public string Experience { get; set; } = "";
    [StringLength(200)] public string Salary { get; set; } = "";
    [Required, StringLength(10000, MinimumLength = 30)] public string Description { get; set; } = "";
    [Required, StringLength(1000), Display(Name = "Official application URL")] public string ApplyLink { get; set; } = "";
    [DataType(DataType.Date), Display(Name = "Start date")] public DateTime StartDate { get; set; } = DateTime.Today;
    [DataType(DataType.Date), Display(Name = "End date")] public DateTime EndDate { get; set; } = DateTime.Today.AddDays(30);
    public byte[] RowVersion { get; set; } = [];
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!JobCategories.All.Contains(Category)) yield return new("Choose a valid category.", [nameof(Category)]);
        if (!Uri.TryCreate(ApplyLink, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.UserInfo != "") yield return new("Enter an HTTPS application URL.", [nameof(ApplyLink)]);
        if (StartDate < new DateTime(2020, 1, 1) || StartDate > DateTime.Today.AddYears(1)) yield return new("Choose a valid start date within the next year.", [nameof(StartDate)]);
        if (EndDate.Date < DateTime.Today || EndDate < StartDate || EndDate > StartDate.AddYears(1)) yield return new("End date must be today or later, on or after the start, and within one year of the start.", [nameof(EndDate)]);
    }
}

public class ResumeServiceRequest
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
    [Required, StringLength(50)] public string Service { get; set; } = "ATS Resume Review";
    [StringLength(2000)] public string Notes { get; set; } = "";
    [StringLength(30)] public string Status { get; set; } = "New";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public static readonly Dictionary<string, string> Services = new()
    {
        ["ATS Resume Review"] = "₹99–₹199",
        ["Professional Resume"] = "₹299–₹499",
        ["Resume + LinkedIn Optimization"] = "₹499–₹799"
    };
}
public class CommerceOverview
{
    public List<EmployerCampaign> Campaigns { get; set; } = [];
    public List<ResumeServiceRequest> Requests { get; set; } = [];
}
public class InvoiceInput
{
    [Required, StringLength(80)] public string Number { get; set; } = "";
    [Required, StringLength(500)] public string SellerDetails { get; set; } = "";
    [Required, StringLength(500)] public string BillTo { get; set; } = "";
    [Required, StringLength(300)] public string Description { get; set; } = "Featured job placement";
    [Range(typeof(decimal), "1", "9999999999.99")] public decimal Amount { get; set; }
}
