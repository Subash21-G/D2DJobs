using System.Security.Claims;
using JobForFresher.Models;
using JobForFresher.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
namespace JobForFresher.Controllers;
[Authorize, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AdminController(ApplicationDbContext db, IWebHostEnvironment environment, ExcelJobImporter excelImporter) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult Login() => User.Identity?.IsAuthenticated == true ? RedirectToAction(nameof(Index)) : View();
    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("admin-login")]
    public async Task<IActionResult> Login(string? userName, string? password)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrEmpty(password) || userName.Length > 200 || password.Length > 1024)
        { ViewBag.Error = "Enter a valid username and password."; return View(); }
        var admin = await db.AdminUsers.FirstOrDefaultAsync(a => a.UserName == userName.Trim());
        var result = admin == null ? PasswordVerificationResult.Failed : AdminPasswords.Verify(admin, password);
        if (admin?.LockoutUntilUtc > DateTime.UtcNow) result = PasswordVerificationResult.Failed;
        if (admin != null && result != PasswordVerificationResult.Failed)
        {
            if (result == PasswordVerificationResult.SuccessRehashNeeded) admin.PasswordHash = AdminPasswords.Hash(admin, password);
            if (string.IsNullOrEmpty(admin.SecurityStamp)) admin.SecurityStamp = Guid.NewGuid().ToString();
            admin.FailedLoginCount = 0; admin.LockoutUntilUtc = null; await db.SaveChangesAsync();
            await SignIn(admin); return RedirectToAction(nameof(Index));
        }
        if (admin != null && (admin.LockoutUntilUtc == null || admin.LockoutUntilUtc <= DateTime.UtcNow))
        {
            // Atomic update: simultaneous failed attempts must not overwrite each other.
            var until = DateTime.UtcNow.AddMinutes(15);
            await db.AdminUsers.Where(a => a.Id == admin.Id).ExecuteUpdateAsync(s => s
                .SetProperty(a => a.FailedLoginCount, a => a.LockoutUntilUtc != null ? 1 : a.FailedLoginCount + 1)
                .SetProperty(a => a.LockoutUntilUtc, a => a.LockoutUntilUtc != null ? (DateTime?)null : a.FailedLoginCount >= 4 ? until : null));
        }
        ViewBag.Error = "Sign-in failed. Check your credentials, or wait 15 minutes if the account is temporarily locked.";
        return View();
    }
    private Task SignIn(AdminUser user) => HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Name, user.UserName), new Claim("security_stamp", user.SecurityStamp) }, CookieAuthenticationDefaults.AuthenticationScheme)),
        new AuthenticationProperties { IsPersistent = false });
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout() { await HttpContext.SignOutAsync(); return RedirectToAction(nameof(Login)); }
    public IActionResult Security() => View(new ChangePasswordInput());
    [HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("admin-login")]
    public async Task<IActionResult> Security(ChangePasswordInput input)
    {
        if (!ModelState.IsValid) return View(input);
        var admin = await db.AdminUsers.FindAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
        if (admin == null) return Challenge();
        if (AdminPasswords.Verify(admin, input.CurrentPassword) == PasswordVerificationResult.Failed)
        { ModelState.AddModelError(nameof(input.CurrentPassword), "Current password is incorrect."); return View(input); }
        if (input.NewPassword == input.CurrentPassword) { ModelState.AddModelError(nameof(input.NewPassword), "Choose a different password."); return View(input); }
        admin.PasswordHash = AdminPasswords.Hash(admin, input.NewPassword); admin.SecurityStamp = Guid.NewGuid().ToString();
        admin.FailedLoginCount = 0; admin.LockoutUntilUtc = null; await db.SaveChangesAsync(); await SignIn(admin);
        TempData["Notice"] = "Password changed. Other signed-in sessions have been revoked."; return RedirectToAction(nameof(Security));
    }
    public async Task<IActionResult> Index(string? search, string? category, string? status, int page = 1, int recentDays = 0, DateTime? addedFrom = null, DateTime? addedTo = null, string dateOrder = "newest", string? metric = null)
    {
        var today = DateTime.Today;
        var untypedJobs = await db.Jobs.Where(j => j.SourceType == null || j.SourceType.Trim() == "" || j.SourceType == JobSourceTypes.Other ||
                !JobSourceTypes.All.Contains(j.SourceType))
            .ToListAsync();
        foreach (var untypedJob in untypedJobs)
        {
            var detectedType = JobSourceTypes.DetectFromOfficialSourceUrl(untypedJob.OfficialSourceUrl, untypedJob.CompanyName);
            if (detectedType != JobSourceTypes.Other) untypedJob.SourceType = detectedType;
        }
        if (db.ChangeTracker.HasChanges()) await db.SaveChangesAsync();

        var totals = await db.Jobs.GroupBy(j => 1).Select(g => new { Total = g.Count(), Featured = g.Count(j => j.IsFeatured), Active = g.Count(j => j.IsActive && (j.AvailableFrom == null || j.AvailableFrom <= today) && (j.ExpiryDate == null || j.ExpiryDate >= today) && (j.WalkInEndDate.HasValue ? j.WalkInEndDate.Value.Date >= today : !j.WalkInDate.HasValue || j.WalkInDate.Value.Date >= today) && (j.SourcePostedDate ?? j.PostedDate).Date > today.AddDays(-30)) }).SingleOrDefaultAsync();
        var expiredCount = await db.Jobs.CountAsync(j => j.IsActive &&
            ((j.ExpiryDate.HasValue && j.ExpiryDate.Value.Date < today) ||
             (j.WalkInEndDate.HasValue ? j.WalkInEndDate.Value.Date < today : j.WalkInDate.HasValue && j.WalkInDate.Value.Date < today) ||
             (j.SourcePostedDate ?? j.PostedDate).Date <= today.AddDays(-30)));
        ViewBag.TotalJobs = totals?.Total ?? 0; ViewBag.FeaturedJobs = totals?.Featured ?? 0; ViewBag.ActiveJobs = totals?.Active ?? 0; ViewBag.ExpiredJobs = expiredCount;
        var jobs = db.Jobs.AsNoTracking(); search = search?.Trim();
        if (!string.IsNullOrEmpty(search)) jobs = jobs.Where(j => j.Title.Contains(search) || j.CompanyName.Contains(search) || j.Location.Contains(search) || j.Skills.Contains(search));
        if (!string.IsNullOrEmpty(category)) jobs = jobs.Where(j => j.Category == category);
        jobs = status switch { "Active" => jobs.Where(j => j.IsActive && (j.AvailableFrom == null || j.AvailableFrom <= today) && (j.ExpiryDate == null || j.ExpiryDate >= today) && (j.WalkInEndDate.HasValue ? j.WalkInEndDate.Value.Date >= today : !j.WalkInDate.HasValue || j.WalkInDate.Value.Date >= today) && (j.SourcePostedDate ?? j.PostedDate).Date > today.AddDays(-30)), "Inactive" => jobs.Where(j => !j.IsActive), "Expired" => jobs.Where(j => j.IsActive && ((j.ExpiryDate.HasValue && j.ExpiryDate.Value.Date < today) || (j.WalkInEndDate.HasValue ? j.WalkInEndDate.Value.Date < today : j.WalkInDate.HasValue && j.WalkInDate.Value.Date < today) || (j.SourcePostedDate ?? j.PostedDate).Date <= today.AddDays(-30))), _ => jobs };
        var recentJobsForDuplicates = db.Jobs.AsNoTracking()
            .Where(j => j.IsActive && (j.AvailableFrom == null || j.AvailableFrom <= today) && (j.ExpiryDate == null || j.ExpiryDate >= today) && (j.SourcePostedDate ?? j.PostedDate).Date > today.AddDays(-30));
        // The same application URL attached to materially different role/company
        // details is a signal for an editor to compare with the source posting.
        var detailsMismatchIds = recentJobsForDuplicates.Where(j => recentJobsForDuplicates.Any(other =>
            other.Id != j.Id && j.ApplyLink != "" && j.ApplyLink == other.ApplyLink &&
            (j.Title.Trim().ToLower() != other.Title.Trim().ToLower() ||
             j.CompanyName.Trim().ToLower() != other.CompanyName.Trim().ToLower() ||
             j.Role.Trim().ToLower() != other.Role.Trim().ToLower() ||
             j.Location.Trim().ToLower() != other.Location.Trim().ToLower())))
            .Select(j => j.Id);
        ViewBag.DetailsMismatchJobs = await detailsMismatchIds.CountAsync();
        var duplicateIds = recentJobsForDuplicates
            .Where(j => recentJobsForDuplicates.Any(other => other.Id != j.Id &&
                (
                    (j.ApplyLink != "" && j.ApplyLink == other.ApplyLink) ||
                    (j.CompanyName.Trim().ToLower().Replace(" private limited", "").Replace(" pvt. ltd.", "").Replace(" pvt ltd", "").Replace(" limited", "").Replace(" ltd.", "").Replace(" ltd", "").Trim() ==
                     other.CompanyName.Trim().ToLower().Replace(" private limited", "").Replace(" pvt. ltd.", "").Replace(" pvt ltd", "").Replace(" limited", "").Replace(" ltd.", "").Replace(" ltd", "").Trim() &&
                     j.Role.Trim() != "" && j.Role.Trim().ToLower() == other.Role.Trim().ToLower() &&
                     j.Location.Trim() != "" && j.Location.Trim().ToLower() == other.Location.Trim().ToLower()) ||
                    (j.ApplicationInstructions != null && j.ApplicationInstructions.Trim() != "" &&
                     other.ApplicationInstructions != null && other.ApplicationInstructions.Trim() == j.ApplicationInstructions.Trim()) ||
                    (j.EditorialNote != null && j.EditorialNote.Trim() != "" &&
                     other.EditorialNote != null && other.EditorialNote.Trim() == j.EditorialNote.Trim())
                )))
            .Select(j => j.Id);
        ViewBag.DuplicateJobs = await duplicateIds.CountAsync();
        var unusableLinkIds = recentJobsForDuplicates.Where(JobQuality.HasUnusableApplicationUrl()).Select(j => j.Id);
        var unusableSourceIds = recentJobsForDuplicates.Where(JobQuality.HasUnusableOfficialSourceUrl()).Select(j => j.Id);
        var internalInstructionIds = recentJobsForDuplicates.Where(JobQuality.HasInternalApplicationFieldText()).Select(j => j.Id);
        var verificationCutoff = JobQuality.VerificationCutoff();
        var notPublishableIds = recentJobsForDuplicates.Where(JobQuality.NeedsReview(verificationCutoff)).Select(j => j.Id);
        ViewBag.UnusableApplicationLinkJobs = await recentJobsForDuplicates.Where(j =>
            unusableLinkIds.Contains(j.Id) || unusableSourceIds.Contains(j.Id) ||
            internalInstructionIds.Contains(j.Id) || notPublishableIds.Contains(j.Id) ||
            duplicateIds.Contains(j.Id)).CountAsync();
        metric = metric is "all" or "active" or "expired" or "featured" or "application" or "duplicate" or "mismatch" ? metric : null;
        jobs = metric switch
        {
            "active" => jobs.Where(j => j.IsActive && (j.AvailableFrom == null || j.AvailableFrom <= today) && (j.ExpiryDate == null || j.ExpiryDate >= today) &&
                (j.SourcePostedDate ?? j.PostedDate).Date > today.AddDays(-30)),
            "expired" => jobs.Where(j => j.IsActive && ((j.ExpiryDate.HasValue && j.ExpiryDate.Value.Date < today) || (j.WalkInEndDate.HasValue ? j.WalkInEndDate.Value.Date < today : j.WalkInDate.HasValue && j.WalkInDate.Value.Date < today) || (j.SourcePostedDate ?? j.PostedDate).Date <= today.AddDays(-30))),
            "featured" => jobs.Where(j => j.IsFeatured),
            "application" => jobs.Where(j =>
                unusableLinkIds.Contains(j.Id) || unusableSourceIds.Contains(j.Id) ||
                internalInstructionIds.Contains(j.Id) || notPublishableIds.Contains(j.Id) ||
                duplicateIds.Contains(j.Id)),
            "duplicate" => jobs.Where(j => duplicateIds.Contains(j.Id)),
            "mismatch" => jobs.Where(j => detailsMismatchIds.Contains(j.Id)),
            "all" => jobs,
            _ => jobs
        };
        recentDays = recentDays is 7 or 30 or 90 ? recentDays : 0;
        dateOrder = dateOrder == "oldest" ? "oldest" : "newest";
        addedFrom = addedFrom?.Date;
        addedTo = addedTo?.Date;
        var invalidDateRange = addedFrom.HasValue && addedTo.HasValue && addedFrom > addedTo;
        if (recentDays > 0) jobs = jobs.Where(j => j.PostedDate >= today.AddDays(1 - recentDays) && j.PostedDate < today.AddDays(1));
        if (addedFrom.HasValue) jobs = jobs.Where(j => j.PostedDate >= addedFrom.Value);
        if (addedTo.HasValue) jobs = jobs.Where(j => j.PostedDate < addedTo.Value.AddDays(1));
        if (invalidDateRange) jobs = jobs.Where(j => false);
        var count = await jobs.CountAsync(); var pages = Math.Max(1, (int)Math.Ceiling(count / 20d)); page = Math.Clamp(page, 1, pages);
        ViewBag.Search = search; ViewBag.Category = category; ViewBag.Status = status; ViewBag.RecentDays = recentDays; ViewBag.Metric = metric;
        ViewBag.AddedFrom = addedFrom?.ToString("yyyy-MM-dd"); ViewBag.AddedTo = addedTo?.ToString("yyyy-MM-dd");
        ViewBag.DateOrder = dateOrder; ViewBag.InvalidDateRange = invalidDateRange; ViewBag.ResultCount = count; ViewBag.Page = page; ViewBag.Pages = pages;
        ViewBag.TopViewedJobs = await db.Jobs.AsNoTracking().OrderByDescending(j => j.ViewsCount).ThenByDescending(j => j.Id).Take(5).ToListAsync();
        var orderedJobs = dateOrder == "oldest"
            ? jobs.OrderBy(j => j.PostedDate).ThenBy(j => j.Id)
            : jobs.OrderByDescending(j => j.PostedDate).ThenByDescending(j => j.Id);
        var pageJobs = await orderedJobs.Skip((page-1)*20).Take(20).ToListAsync();
        var pagePubliclyDiscoverableIds = new HashSet<int>();
        if (pageJobs.Count > 0)
        {
            var pageIds = pageJobs.Select(job => job.Id).ToArray();
            pagePubliclyDiscoverableIds = (await recentJobsForDuplicates.Where(JobQuality.ReadyForIndex(verificationCutoff)).Where(j => !duplicateIds.Contains(j.Id)).Select(j => j.Id).Where(id => pageIds.Contains(id)).ToListAsync()).ToHashSet();
        }
        ViewBag.PubliclyDiscoverableJobIds = pagePubliclyDiscoverableIds;
        return View(pageJobs);
    }
    [HttpGet]
    public IActionResult Import() => View(new JobImportViewModel());

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Import(JobImportViewModel input, CancellationToken cancellationToken)
    {
        if (input.File == null || input.File.Length == 0)
        {
            input.Errors.Add("Choose an .xlsx workbook to upload.");
            return View(input);
        }
        if (!string.Equals(Path.GetExtension(input.File.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            input.Errors.Add("Only .xlsx Excel workbooks are supported.");
            return View(input);
        }
        if (input.File.Length > 10 * 1024 * 1024)
        {
            input.Errors.Add("The workbook must be 10 MB or smaller.");
            return View(input);
        }

        try
        {
            await using var stream = input.File.OpenReadStream();
            var jobs = excelImporter.Read(stream, input.Errors);
            input.RejectedCount = input.Errors.Select(error => System.Text.RegularExpressions.Regex.Match(error, @"^Row (\d+):"))
                .Where(match => match.Success).Select(match => match.Groups[1].Value).Distinct().Count();
            var workbookErrors = input.Errors.Any(error => !error.StartsWith("Row ", StringComparison.Ordinal));
            if (workbookErrors || jobs.Count == 0)
            {
                if (jobs.Count == 0 && input.Errors.Count == 0) input.Errors.Add("The workbook did not contain any data rows.");
                input.Summary = $"Accepted {jobs.Count}; rejected {input.RejectedCount}; imported 0 listings.";
                return View(input);
            }

            foreach (var job in jobs)
            {
                job.Slug = Slug(job.Title);
                job.PostedDate = DateTime.Now;
                job.ImportedUtc = DateTime.UtcNow;
                job.IsActive = false;
                job.LastVerifiedUtc = null;
            }
            await db.Jobs.AddRangeAsync(jobs, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            input.ImportedCount = jobs.Count;
            input.Summary = $"Accepted {jobs.Count}; rejected {input.RejectedCount}; imported {jobs.Count} draft listing(s) for source and editorial review.";
            return View(input);
        }
        catch (InvalidDataException)
        {
            input.Errors.Add("The file is not a valid .xlsx workbook.");
            return View(input);
        }
        catch (System.Xml.XmlException)
        {
            input.Errors.Add("The workbook contains invalid worksheet data.");
            return View(input);
        }
    }
    public IActionResult Create() => View(new Job());
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> Create(Job input, IFormFile? logoFile)
    {
        if (!ModelState.IsValid) return View(input);
        var logo = await SaveLogo(logoFile);
        if (!ModelState.IsValid) return View(input);
        var job = new Job(); CopyFields(input, job); job.CompanyLogo = logo;
        job.Slug = Slug(job.Title); job.PostedDate = DateTime.Now; job.ImportedUtc = DateTime.UtcNow;
        db.Jobs.Add(job); await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Edit(int id)
    {
        var job = await db.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id);
        return job == null ? NotFound() : View(job);
    }
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> Edit(Job input, IFormFile? logoFile)
    {
        var job = await db.Jobs.FindAsync(input.Id); if (job == null) return NotFound();
        input.CompanyLogo = job.CompanyLogo;
        if (!ModelState.IsValid) return View(input);
        var logo = await SaveLogo(logoFile); if (!ModelState.IsValid) return View(input);
        var previousLogo = job.CompanyLogo;
        CopyFields(input, job); if (logo != null) job.CompanyLogo = logo;
        job.Slug ??= Slug(job.Title);
        await db.SaveChangesAsync(); if (logo != null) DeleteLogo(previousLogo);
        return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var job = await db.Jobs.FindAsync(id);
        if (job != null) { db.Jobs.Remove(job); await db.SaveChangesAsync(); DeleteLogo(job.CompanyLogo); }
        return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(16 * 1024), RequestFormLimits(ValueCountLimit = 32)]
    public async Task<IActionResult> DeleteSelected(int[]? jobIds, CancellationToken cancellationToken)
    {
        if (jobIds == null || jobIds.Length == 0)
        {
            TempData["Notice"] = "Select at least one job to delete.";
            return RedirectToAction(nameof(Index));
        }
        // The overview displays at most 20 rows per page. Keep this action page-scoped.
        if (jobIds.Length > 20 || jobIds.Any(id => id <= 0))
            return BadRequest("Select up to 20 valid jobs from one overview page.");

        var ids = jobIds.Distinct().ToArray();
        var jobs = await db.Jobs.Where(job => ids.Contains(job.Id)).ToListAsync(cancellationToken);
        if (jobs.Count == 0)
        {
            TempData["Notice"] = "No selected jobs were found. Refresh the overview and try again.";
            return RedirectToAction(nameof(Index));
        }
        db.Jobs.RemoveRange(jobs);
        await db.SaveChangesAsync(cancellationToken);
        foreach (var job in jobs) DeleteLogo(job.CompanyLogo);
        TempData["Notice"] = $"Deleted {jobs.Count} selected job(s).";
        return RedirectToAction(nameof(Index));
    }
    private static string Slug(string title) => System.Text.RegularExpressions.Regex.Replace(title.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-') + "-" + Guid.NewGuid().ToString("N")[..10];
    private static void CopyFields(Job a, Job b)
    {
        b.Title = a.Title; b.CompanyName = a.CompanyName ?? ""; b.Category = JobCategories.Normalize(a.Category) ?? a.Category ?? ""; b.SubCategory = a.SubCategory ?? "";
        b.Role = a.Role ?? ""; b.Location = a.Location ?? ""; b.Experience = a.Experience ?? ""; b.Salary = a.Salary ?? "";
        b.JobType = a.JobType ?? ""; b.Qualification = a.Qualification ?? ""; b.Skills = a.Skills ?? ""; b.Description = a.Description ?? "";
        b.ApplyLink = a.ApplyLink ?? ""; b.IsFeatured = a.IsFeatured; b.ExpiryDate = a.ExpiryDate; b.IsActive = a.IsActive;
        b.BatchFrom = a.BatchFrom; b.BatchTo = a.BatchTo; b.Eligibility = a.Eligibility; b.SelectionProcess = a.SelectionProcess;
        b.ApplicationInstructions = a.ApplicationInstructions; b.DocumentsRequired = a.DocumentsRequired; b.EditorialNote = a.EditorialNote;
        b.SourceType = JobSourceTypes.Resolve(a.SourceType, a.OfficialSourceUrl, a.CompanyName);
        b.SourcePostedDate = a.SourcePostedDate; b.WalkInDate = a.WalkInDate; b.WalkInStartDate = a.WalkInStartDate; b.WalkInEndDate = a.WalkInEndDate;
        b.WalkInVenue = a.WalkInVenue; b.OfficialSourceUrl = a.OfficialSourceUrl;
        b.LastVerifiedUtc = a.LastVerifiedUtc.HasValue
            ? DateTime.SpecifyKind(a.LastVerifiedUtc.Value.Date, DateTimeKind.Utc)
            : null;
    }
    private async Task<string?> SaveLogo(IFormFile? file)
    {
        if (file == null || file.Length == 0) return null;
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var bytes = new byte[12]; await using var stream = file.OpenReadStream(); var read = await stream.ReadAsync(bytes);
        bool valid = extension switch { ".png" => read >= 8 && bytes.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}), ".jpg" or ".jpeg" => read >= 3 && bytes[0]==255 && bytes[1]==216 && bytes[2]==255, ".webp" => read >= 12 && System.Text.Encoding.ASCII.GetString(bytes,0,4)=="RIFF" && System.Text.Encoding.ASCII.GetString(bytes,8,4)=="WEBP", _ => false };
        if (!valid || file.Length > 2*1024*1024) { ModelState.AddModelError("logoFile", "Upload a valid JPG, PNG or WEBP image up to 2 MB."); return null; }
        var folder = Path.Combine(environment.WebRootPath, "uploads", "logos"); Directory.CreateDirectory(folder);
        var name = Guid.NewGuid().ToString("N") + extension; await using var output = System.IO.File.Create(Path.Combine(folder,name)); await file.CopyToAsync(output); return name;
    }
    private void DeleteLogo(string? name)
    {
        if (string.IsNullOrEmpty(name) || name != Path.GetFileName(name)) return;
        var path = Path.Combine(environment.WebRootPath,"uploads","logos",name); if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
    }
}
