using System.Text.Json;
using JobForFresher.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
namespace JobForFresher.Controllers;
[Authorize, ResponseCache(NoStore=true, Location=ResponseCacheLocation.None)]
public class SettingsController(IOptionsSnapshot<SiteOptions> site, IOptionsSnapshot<AdvertisingOptions> ads, IWebHostEnvironment environment) : Controller
{
    public IActionResult Index() => View(new SiteSetupInput { Site=site.Value, AdsEnabled=ads.Value.Enabled, SiteApproved=ads.Value.SiteApproved, ConsentConfigured=ads.Value.ConsentConfigured, PublisherId=ads.Value.PublisherId, ListingSlot=ads.Value.Slots.GetValueOrDefault("ListingInline"), DetailSlot=ads.Value.Slots.GetValueOrDefault("JobSidebar") });
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SiteSetupInput input)
    {
        if (!ModelState.IsValid) return View(input);
        var settings=new { SiteSettings=input.Site, Advertising=new AdvertisingOptions { Enabled=input.AdsEnabled, SiteApproved=input.SiteApproved, ConsentConfigured=input.ConsentConfigured, PublisherId=input.PublisherId ?? "", Slots=new() { ["ListingInline"]=input.ListingSlot ?? "", ["JobSidebar"]=input.DetailSlot ?? "" } } };
        var folder=Path.Combine(environment.ContentRootPath,"App_Data"); Directory.CreateDirectory(folder);
        var temporary=Path.Combine(folder,Guid.NewGuid()+".tmp");
        try { await System.IO.File.WriteAllTextAsync(temporary,JsonSerializer.Serialize(settings,new JsonSerializerOptions {WriteIndented=true})); System.IO.File.Move(temporary,Path.Combine(folder,"site-settings.json"),true); }
        finally { if(System.IO.File.Exists(temporary)) System.IO.File.Delete(temporary); }
        TempData["Notice"]="Site setup saved. Changes reload automatically; deployment environment variables take precedence.";
        return RedirectToAction(nameof(Index));
    }
}