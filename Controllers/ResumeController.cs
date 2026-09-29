using JobForFresher.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace JobForFresher.Controllers;
[Route("resume-builder")]
public class ResumeController(ApplicationDbContext db) : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
    [HttpGet("services")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Services() => View(new ResumeServiceRequest());
    [HttpPost("services"), ValidateAntiForgeryToken, EnableRateLimiting("public-submission")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Services([Bind("Name,Email,Service,Notes")] ResumeServiceRequest input)
    {
        if (!ResumeServiceRequest.Services.ContainsKey(input.Service ?? "")) ModelState.AddModelError(nameof(input.Service), "Choose a service from the list.");
        if (!ModelState.IsValid) return View(input);
        db.ResumeServiceRequests.Add(input); await db.SaveChangesAsync();
        TempData["Notice"] = "Request received. Our team will email you to confirm the scope and price before any work or payment.";
        return RedirectToAction(nameof(Services));
    }
}
