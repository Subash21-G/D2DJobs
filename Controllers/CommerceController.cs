using JobForFresher.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobForFresher.Controllers;
[Authorize, Route("admin/commerce"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class CommerceController(ApplicationDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index() => View(new CommerceOverview
    {
        Campaigns = await db.EmployerCampaigns.AsNoTracking().Include(c => c.EmployerAccount).Include(c => c.Job).Include(c => c.Invoices).OrderByDescending(c => c.UpdatedUtc).ToListAsync(),
        Requests = await db.ResumeServiceRequests.AsNoTracking().OrderByDescending(r => r.CreatedUtc).ToListAsync()
    });
    [HttpPost("campaigns/{id:int}/review"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(int id, string decision, string? note, byte[] rowVersion)
    {
        if (decision is not ("Approve" or "Reject" or "Pause") || note?.Length > 1000 || rowVersion == null) return BadRequest();
        var c = await db.EmployerCampaigns.Include(c => c.Job).Include(c => c.EmployerAccount).FirstOrDefaultAsync(c => c.Id == id);
        if (c == null) return NotFound();
        db.Entry(c).Property(c => c.RowVersion).OriginalValue = rowVersion;
        if (decision == "Approve")
        {
            if (c.EndDate < DateTime.Today) { TempData["Notice"] = "The campaign has ended. Ask the employer to update its dates before approval."; return RedirectToAction(nameof(Index)); }
            c.Job ??= new Job { Slug = "employer-" + Guid.NewGuid().ToString("N"), PostedDate = DateTime.Now };
            c.Job.Title = c.Title; c.Job.CompanyName = c.EmployerAccount.CompanyName; c.Job.Category = c.Category;
            c.Job.Location = c.Location; c.Job.Qualification = c.Qualification; c.Job.Experience = c.Experience; c.Job.Salary = c.Salary;
            c.Job.Description = c.Description; c.Job.ApplyLink = c.ApplyLink; c.Job.Role = c.Title;
            c.Job.AvailableFrom = c.StartDate; c.Job.ExpiryDate = c.EndDate; c.Job.IsActive = true;
            c.Job.IsFeatured = c.UpgradeStatus == "Approved";
            c.ReviewStatus = "Approved";
        }
        else
        {
            c.ReviewStatus = decision == "Reject" ? "Rejected" : "Paused";
            if (c.Job != null) c.Job.IsActive = false;
        }
        c.ReviewNote = note?.Trim() ?? ""; c.UpdatedUtc = DateTime.UtcNow;
        return await Save("Campaign review saved.");
    }
    [HttpPost("campaigns/{id:int}/upgrade"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Upgrade(int id, string decision, byte[] rowVersion)
    {
        if (decision is not ("Approved" or "Declined") || rowVersion == null) return BadRequest();
        var c = await db.EmployerCampaigns.Include(c => c.Job).FirstOrDefaultAsync(c => c.Id == id);
        if (c == null) return NotFound();
        if (decision == "Approved" && (c.ReviewStatus != "Approved" || c.Job == null || c.EndDate < DateTime.Today))
        { TempData["Notice"] = "Approve a current campaign before activating its featured upgrade."; return RedirectToAction(nameof(Index)); }
        db.Entry(c).Property(c => c.RowVersion).OriginalValue = rowVersion;
        c.UpgradeStatus = decision; c.UpdatedUtc = DateTime.UtcNow;
        if (c.Job != null) c.Job.IsFeatured = decision == "Approved";
        return await Save("Featured upgrade updated.");
    }
    [HttpPost("campaigns/{id:int}/invoice"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Invoice(int id, InvoiceInput input)
    {
        if (!await db.EmployerCampaigns.AnyAsync(c => c.Id == id)) return NotFound();
        if (!ModelState.IsValid || decimal.Round(input.Amount, 2) != input.Amount)
        { TempData["Notice"] = "Invoice not created. Complete all invoice fields and enter a positive INR amount with at most two decimals."; return RedirectToAction(nameof(Index)); }
        var number = input.Number.Trim();
        if (await db.CampaignInvoices.AnyAsync(i => i.Number == number))
        { TempData["Notice"] = "That invoice number is already in use."; return RedirectToAction(nameof(Index)); }
        db.CampaignInvoices.Add(new CampaignInvoice { EmployerCampaignId = id, Number = number, SellerDetails = input.SellerDetails.Trim(), BillTo = input.BillTo.Trim(), Description = input.Description.Trim(), Amount = input.Amount });
        try { await db.SaveChangesAsync(); TempData["Notice"] = "Invoice issued and available in the employer dashboard."; }
        catch (DbUpdateException) { TempData["Notice"] = "Invoice could not be issued. Check that its number is unique and try again."; }
        return RedirectToAction(nameof(Index));
    }
    [HttpPost("invoices/{id:int}/paid"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Paid(int id)
    {
        var invoice = await db.CampaignInvoices.FindAsync(id); if (invoice == null) return NotFound();
        invoice.PaidUtc ??= DateTime.UtcNow; return await Save("Invoice marked paid after your payment confirmation.");
    }
    [HttpPost("resume-requests/{id:int}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ResumeRequest(int id, string status)
    {
        if (status is not ("New" or "Contacted" or "In progress" or "Completed" or "Cancelled")) return BadRequest();
        var request = await db.ResumeServiceRequests.FindAsync(id); if (request == null) return NotFound();
        request.Status = status; return await Save("Resume service request updated.");
    }
    private async Task<IActionResult> Save(string notice)
    {
        try { await db.SaveChangesAsync(); TempData["Notice"] = notice; }
        catch (DbUpdateConcurrencyException) { TempData["Notice"] = "This campaign changed while you were reviewing it. Review the latest details and try again."; }
        return RedirectToAction(nameof(Index));
    }
}
