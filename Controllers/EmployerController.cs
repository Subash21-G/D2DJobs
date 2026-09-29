using System.Security.Claims;
using JobForFresher.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace JobForFresher.Controllers;
[Route("employer"), Authorize(AuthenticationSchemes = "Employer")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class EmployerController(ApplicationDbContext db) : Controller
{
    private int AccountId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static readonly PasswordHasher<EmployerAccount> Hasher = new();
    [AllowAnonymous, HttpGet("register")]
    public IActionResult Register() => View(new EmployerRegisterInput());
    [AllowAnonymous, HttpPost("register"), ValidateAntiForgeryToken, EnableRateLimiting("public-submission")]
    public async Task<IActionResult> Register(EmployerRegisterInput input)
    {
        if (!ModelState.IsValid) return View(input);
        var normalized = input.Email.Trim().ToUpperInvariant();
        if (await db.EmployerAccounts.AnyAsync(a => a.NormalizedEmail == normalized))
        { ModelState.AddModelError("", "Unable to register with this email. If you already have an account, sign in."); return View(input); }
        var account = new EmployerAccount { CompanyName = input.CompanyName.Trim(), Email = input.Email.Trim(), NormalizedEmail = normalized };
        account.PasswordHash = Hasher.HashPassword(account, input.Password);
        db.EmployerAccounts.Add(account);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { ModelState.AddModelError("", "Unable to create the account. Please try signing in or try again later."); return View(input); }
        await SignIn(account); return RedirectToAction(nameof(Index));
    }
    [AllowAnonymous, HttpGet("login")]
    public IActionResult Login() => View(new EmployerLoginInput());
    [AllowAnonymous, HttpPost("login"), ValidateAntiForgeryToken, EnableRateLimiting("admin-login")]
    public async Task<IActionResult> Login(EmployerLoginInput input)
    {
        if (!ModelState.IsValid) return View(input);
        var email = input.Email.Trim().ToUpperInvariant();
        var account = await db.EmployerAccounts.FirstOrDefaultAsync(a => a.NormalizedEmail == email);
        var result = account == null || account.LockoutUntilUtc > DateTime.UtcNow ? PasswordVerificationResult.Failed : Hasher.VerifyHashedPassword(account, account.PasswordHash, input.Password);
        if (account != null && result != PasswordVerificationResult.Failed)
        {
            if (result == PasswordVerificationResult.SuccessRehashNeeded) account.PasswordHash = Hasher.HashPassword(account, input.Password);
            account.FailedLoginCount = 0; account.LockoutUntilUtc = null;
            await db.SaveChangesAsync(); await SignIn(account); return RedirectToAction(nameof(Index));
        }
        if (account != null && (account.LockoutUntilUtc == null || account.LockoutUntilUtc <= DateTime.UtcNow))
        {
            var until = DateTime.UtcNow.AddMinutes(15);
            await db.EmployerAccounts.Where(a => a.Id == account.Id).ExecuteUpdateAsync(s => s
                .SetProperty(a => a.FailedLoginCount, a => a.LockoutUntilUtc != null ? 1 : a.FailedLoginCount + 1)
                .SetProperty(a => a.LockoutUntilUtc, a => a.LockoutUntilUtc != null ? (DateTime?)null : a.FailedLoginCount >= 4 ? until : null));
        }
        ModelState.AddModelError("", "Sign-in failed. Check your details, or wait 15 minutes if temporarily locked."); return View(input);
    }
    private Task SignIn(EmployerAccount account) => HttpContext.SignInAsync("Employer", new ClaimsPrincipal(new ClaimsIdentity([
        new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()), new Claim(ClaimTypes.Name, account.CompanyName), new Claim("security_stamp", account.SecurityStamp)
    ], "Employer")), new AuthenticationProperties { IsPersistent = false });
    [HttpPost("logout"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout() { await HttpContext.SignOutAsync("Employer"); return RedirectToAction(nameof(Login)); }
    [HttpGet("")]
    public async Task<IActionResult> Index() => View(await db.EmployerCampaigns.AsNoTracking().Include(c => c.Job).Include(c => c.Invoices).Where(c => c.EmployerAccountId == AccountId).OrderByDescending(c => c.CreatedUtc).ToListAsync());
    [HttpGet("jobs/new")]
    public IActionResult Create() => View("Edit", new CampaignInput());
    [HttpPost("jobs/new"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CampaignInput input)
    {
        if (!ModelState.IsValid) return View("Edit", input);
        var campaign = new EmployerCampaign { EmployerAccountId = AccountId }; Copy(input, campaign);
        db.EmployerCampaigns.Add(campaign); await db.SaveChangesAsync();
        TempData["Notice"] = "Job submitted for review. It will become public after approval and on its start date.";
        return RedirectToAction(nameof(Index));
    }
    [HttpGet("jobs/{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var c = await db.EmployerCampaigns.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && c.EmployerAccountId == AccountId);
        if (c == null) return NotFound();
        ViewBag.CampaignId = id;
        return View(new CampaignInput { Title = c.Title, Category = c.Category, Location = c.Location, Qualification = c.Qualification, Experience = c.Experience, Salary = c.Salary, Description = c.Description, ApplyLink = c.ApplyLink, StartDate = c.StartDate, EndDate = c.EndDate, RowVersion = c.RowVersion });
    }
    [HttpPost("jobs/{id:int}/edit"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CampaignInput input)
    {
        var campaign = await db.EmployerCampaigns.Include(c => c.Job).FirstOrDefaultAsync(c => c.Id == id && c.EmployerAccountId == AccountId);
        if (campaign == null) return NotFound();
        ViewBag.CampaignId = id;
        if (!ModelState.IsValid) return View(input);
        db.Entry(campaign).Property(c => c.RowVersion).OriginalValue = input.RowVersion;
        Copy(input, campaign); campaign.ReviewStatus = "Pending review"; campaign.ReviewNote = "";
        if (campaign.Job != null) campaign.Job.IsActive = false;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { TempData["Notice"] = "This campaign changed in another session. Please reopen it and try again."; return RedirectToAction(nameof(Index)); }
        TempData["Notice"] = "Changes submitted for review. The listing is paused until approval."; return RedirectToAction(nameof(Index));
    }
    private static void Copy(CampaignInput i, EmployerCampaign c)
    {
        c.Title = i.Title.Trim(); c.Category = i.Category; c.Location = i.Location.Trim(); c.Qualification = i.Qualification.Trim();
        c.Experience = i.Experience.Trim(); c.Salary = i.Salary?.Trim() ?? ""; c.Description = i.Description.Trim(); c.ApplyLink = i.ApplyLink.Trim();
        c.StartDate = i.StartDate.Date; c.EndDate = i.EndDate.Date; c.UpdatedUtc = DateTime.UtcNow;
    }
    [HttpPost("jobs/{id:int}/upgrade"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Upgrade(int id)
    {
        var campaign = await db.EmployerCampaigns.FirstOrDefaultAsync(c => c.Id == id && c.EmployerAccountId == AccountId);
        if (campaign == null) return NotFound();
        if (campaign.UpgradeStatus is "None" or "Declined")
        {
            campaign.UpgradeStatus = "Requested"; campaign.UpdatedUtc = DateTime.UtcNow;
            try { await db.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { TempData["Notice"] = "The campaign changed. Please check its current upgrade status."; return RedirectToAction(nameof(Index)); }
        }
        TempData["Notice"] = "Featured request received. We will contact you to agree pricing and dates before activation.";
        return RedirectToAction(nameof(Index));
    }
    [HttpGet("invoices/{id:int}")]
    public async Task<IActionResult> Invoice(int id)
    {
        var invoice = await db.CampaignInvoices.AsNoTracking().Include(i => i.EmployerCampaign).FirstOrDefaultAsync(i => i.Id == id && i.EmployerCampaign.EmployerAccountId == AccountId);
        return invoice == null ? NotFound() : View(invoice);
    }
}
