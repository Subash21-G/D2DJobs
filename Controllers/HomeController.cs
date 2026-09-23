using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;
using JobForFresher.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobForFresher.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IDataProtector _savedJobsProtector;
    private readonly IConfiguration _configuration;
    private bool IsAdminSession => User.Identity?.IsAuthenticated == true;
    private string Origin => Uri.TryCreate(_configuration["SiteSettings:SiteUrl"], UriKind.Absolute, out var url) && url.Scheme == "https" ? url.GetLeftPart(UriPartial.Authority) : $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
    private const string SavedCookie = "JobForFresher.SavedJobs";
    public HomeController(ApplicationDbContext context, IDataProtectionProvider protection, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        _savedJobsProtector = protection.CreateProtector("JobForFresher.SavedJobs.v1");
    }
    private IQueryable<Job> AvailableJobs() => _context.Jobs.AsNoTracking()
        .Where(j => j.IsActive && (j.ExpiryDate == null || j.ExpiryDate >= DateTime.Today));
    private static IQueryable<Job> CategoryJobs(IQueryable<Job> query, string category) => category switch
    {
        "Off Campus" => query.Where(j => j.Category == "Off Campus" || j.SubCategory == "Off Campus Drive"),
        "Walk-in" => query.Where(j => j.Category == "Walk-in" || j.SubCategory == "Walk-in Drive"),
        "Internship Programs" or "Internship" => query.Where(j => j.Category == "Internship Programs" || j.Category == "Internship"),
        "Work From Home Jobs" or "WFH Jobs" => query.Where(j => j.Category == "Work From Home Jobs" || j.Category == "WFH Jobs" || j.JobType == "Remote"),
        _ => query.Where(j => j.Category == category)
    };

    public async Task<IActionResult> Index(string? search, string? category, string? location, string? experience, string? qualification, int page = 1, string sort = "latest", int? batch = null)
    {
        const int pageSize = 12;
        search = search?.Trim(); location = location?.Trim(); qualification = qualification?.Trim();
        category = category switch { "Internship" => "Internship Programs", "WFH Jobs" => "Work From Home Jobs", _ => category };
        if (batch.HasValue && (batch < 1990 || batch > 2100)) return BadRequest("Choose a graduation year between 1990 and 2100.");
        var categorySlug = JobCategories.Slug(category);
        if (!string.IsNullOrEmpty(category) && categorySlug == null) return MissingJob();
        if (categorySlug != null && !Request.Path.StartsWithSegments("/jobs"))
            return RedirectToRoutePermanent("category", new { categorySlug, search, location, experience, qualification, page, sort, batch });
        ViewBag.Batch = batch;
        ViewData["MetaDescription"] = string.IsNullOrEmpty(category) ? "Browse current fresher jobs, off campus drives and internships. Filter by graduation batch, qualification and location." : $"Explore current {category} across India. Check graduation batch, eligibility, closing dates and official application details.";
        ViewData["Canonical"] = Origin + (categorySlug == null ? "/" : "/jobs/" + categorySlug);
        var available = AvailableJobs();
        var jobs = available;
        if (!string.IsNullOrWhiteSpace(search))
            jobs = jobs.Where(j => EF.Functions.Like(j.Title, $"%{search}%") || EF.Functions.Like(j.CompanyName, $"%{search}%") || EF.Functions.Like(j.Location, $"%{search}%") || EF.Functions.Like(j.Skills, $"%{search}%"));
        if (!string.IsNullOrWhiteSpace(category)) jobs = CategoryJobs(jobs, category);
        if (!string.IsNullOrWhiteSpace(location)) jobs = jobs.Where(j => EF.Functions.Like(j.Location, $"%{location}%"));
        if (!string.IsNullOrWhiteSpace(experience)) jobs = jobs.Where(j => EF.Functions.Like(j.Experience, $"%{experience}%"));
        if (!string.IsNullOrWhiteSpace(qualification)) jobs = jobs.Where(j => j.Qualification.Contains(qualification));
        if (batch.HasValue) jobs = jobs.Where(j => j.BatchFrom <= batch.Value && j.BatchTo >= batch.Value);
        var total = await jobs.CountAsync();
        var pages = (int)Math.Ceiling(total / (double)pageSize);
        page = Math.Clamp(page, 1, Math.Max(1, pages));
        sort = sort is "popular" or "closing" ? sort : "latest";
        var ordered = sort switch
        {
            "popular" => jobs.OrderByDescending(j => j.ApplyClicks).ThenByDescending(j => j.Id),
            "closing" => jobs.OrderBy(j => j.ExpiryDate == null).ThenBy(j => j.ExpiryDate).ThenByDescending(j => j.Id),
            _ => jobs.OrderByDescending(j => j.PostedDate).ThenByDescending(j => j.Id)
        };
        var result = await ordered.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        ViewBag.CurrentPage = page; ViewBag.TotalPages = pages; ViewBag.ResultCount = total;
        ViewBag.Search = search; ViewBag.Category = category; ViewBag.Location = location; ViewBag.Experience = experience; ViewBag.Sort = sort;
        ViewBag.Qualification = qualification;
        var totals = await available.GroupBy(j => 1).Select(g => new {
            Total = g.Count(), Freshers = g.Count(j => j.Category == "Freshers Jobs"),
            OffCampus = g.Count(j => j.Category == "Off Campus" || j.SubCategory == "Off Campus Drive"),
            WalkIn = g.Count(j => j.Category == "Walk-in" || j.SubCategory == "Walk-in Drive"),
            IT = g.Count(j => j.Category == "IT Jobs"), Government = g.Count(j => j.Category == "Government Jobs"),
            Bank = g.Count(j => j.Category == "Bank Jobs"),
            Internship = g.Count(j => j.Category == "Internship Programs" || j.Category == "Internship"),
            Remote = g.Count(j => j.Category == "Work From Home Jobs" || j.Category == "WFH Jobs" || j.JobType == "Remote"),
            Core = g.Count(j => j.Category == "Core Engineering Jobs"), Bpo = g.Count(j => j.Category == "BPO / Support Jobs")
        }).FirstOrDefaultAsync();
        ViewBag.TotalJobs = totals?.Total ?? 0;
        ViewBag.TrendingJobs = await available.OrderByDescending(j => j.ViewsCount).ThenByDescending(j => j.PostedDate).Take(5).ToListAsync();
        ViewBag.PopularJobs = await available.OrderByDescending(j => j.ApplyClicks).ThenByDescending(j => j.PostedDate).Take(3).ToListAsync();
        var counts = new Dictionary<string, int> {
            ["Freshers Jobs"] = totals?.Freshers ?? 0, ["Off Campus"] = totals?.OffCampus ?? 0,
            ["Walk-in"] = totals?.WalkIn ?? 0, ["IT Jobs"] = totals?.IT ?? 0,
            ["Government Jobs"] = totals?.Government ?? 0, ["Bank Jobs"] = totals?.Bank ?? 0,
            ["Internship Programs"] = totals?.Internship ?? 0, ["Work From Home Jobs"] = totals?.Remote ?? 0,
            ["Core Engineering Jobs"] = totals?.Core ?? 0, ["BPO / Support Jobs"] = totals?.Bpo ?? 0
        };
        var sections = new Dictionary<string, List<Job>>();
        var showSections = page == 1 && sort == "latest" && !batch.HasValue && string.IsNullOrWhiteSpace(search + category + location + experience + qualification);
        foreach (var name in JobCategories.All)
        {
            var categoryQuery = CategoryJobs(available, name);

            if (showSections && counts[name] > 0 && new[] { "Off Campus", "Walk-in", "IT Jobs", "Government Jobs", "Bank Jobs", "Internship Programs", "Freshers Jobs" }.Contains(name))
                sections[name] = await categoryQuery.OrderByDescending(j => j.PostedDate).ThenByDescending(j => j.Id).Take(4).ToListAsync();
        }
        ViewBag.CategoryCounts = counts; ViewBag.CategorySections = sections;
        return View("Index", result);
    }

    [HttpGet("/jobs/{categorySlug}", Name = "category")]
    public Task<IActionResult> Category(string categorySlug, string? search, string? location, string? experience, string? qualification, int page = 1, string sort = "latest", int? batch = null)
    {
        var category = JobCategories.Name(categorySlug);
        return category == null ? Task.FromResult(MissingJob()) : Index(search, category, location, experience, qualification, page, sort, batch);
    }
    [HttpGet("/job-alerts")]
    public IActionResult Alerts() => View(_configuration.GetSection("SiteSettings").Get<SiteOptions>() ?? new());
    [HttpGet("/jobs/feed.xml", Name = "job-feed")]
    public async Task<IActionResult> Feed(string? categorySlug, int? batch)
    {
        var jobs = AvailableJobs();
        if (!string.IsNullOrEmpty(categorySlug)) { var category = JobCategories.Name(categorySlug); if (category == null) return NotFound(); jobs = CategoryJobs(jobs, category); }
        if (batch.HasValue) { if (batch < 1990 || batch > 2100) return BadRequest(); jobs = jobs.Where(j => j.BatchFrom <= batch && j.BatchTo >= batch); }
        var entries = await jobs.OrderByDescending(j => j.PostedDate).ThenByDescending(j => j.Id).Take(30).ToListAsync();
        var channel = new XElement("channel", new XElement("title", "D2DJobs job alerts"), new XElement("link", Origin), new XElement("description", "Latest active openings. Verify eligibility on the employer's official website."), new XElement("language", "en-IN"));
        foreach(var job in entries.Where(j => !string.IsNullOrEmpty(j.Slug)))
        {
            var url = Origin + "/job/" + Uri.EscapeDataString(job.Slug!);
            channel.Add(new XElement("item", new XElement("title", job.Title), new XElement("link",url), new XElement("guid",url), new XElement("description", $"{job.CompanyName} | {job.Location} | {job.Qualification}"), new XElement("pubDate",job.PostedDate.ToUniversalTime().ToString("r"))));
        }
        return Content(new XDocument(new XElement("rss",new XAttribute("version","2.0"),channel)).ToString(),"application/rss+xml; charset=utf-8");
    }
    private IActionResult MissingJob()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View("NotFound");
    }
    [HttpGet("/job/resolve", Name = "resolve-job")]
    public async Task<IActionResult> ResolveJob(string? company, string? title)
    {
        if (string.IsNullOrWhiteSpace(company) || string.IsNullOrWhiteSpace(title) || company.Length > 500 || title.Length > 500)
            return MissingJob();
        company = company.Trim(); title = title.Trim();
        var matches = await _context.Jobs.AsNoTracking()
            .Where(j => j.IsActive && j.CompanyName == company && j.Title == title && j.Slug != null && j.Slug != "")
            .Select(j => j.Slug).Take(2).ToListAsync();
        // Never choose an unrelated or ambiguous listing across databases.
        return matches.Count == 1
            ? RedirectToRoute("job-details", new { slug = matches[0] })
            : MissingJob();
    }

    public async Task<IActionResult> Details(string slug)
    {
        var job = await _context.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.Slug == slug && j.IsActive);
        if (job == null) return MissingJob();
        if (!IsAdminSession)
        {
            await _context.Jobs.Where(j => j.Id == job.Id).ExecuteUpdateAsync(set => set.SetProperty(j => j.ViewsCount, j => j.ViewsCount + 1));
            job.ViewsCount++;
        }
        ViewData["Canonical"] = Url.RouteUrl("job-details", new { slug = job.Slug }, "https", "d2djobs.in");
        ViewBag.RelatedJobs = await AvailableJobs().Where(j => j.Category == job.Category && j.Id != job.Id).OrderByDescending(j => j.PostedDate).Take(4).ToListAsync();
        ViewBag.IsSaved = ReadSavedIds().Contains(job.Id);
        ViewData["Title"] = $"{job.Title} at {job.CompanyName} | D2DJobs";
        ViewData["MetaDescription"] = $"{job.Title} at {job.CompanyName} in {job.Location}. Check eligibility, skills and application details.";
        return View(job);
    }
    public async Task<IActionResult> ApplyClick(int id)
    {
        var job = await AvailableJobs().FirstOrDefaultAsync(j => j.Id == id);
        if (job == null) return MissingJob();
        if (!Uri.TryCreate(job.ApplyLink, UriKind.Absolute, out var link) || (link.Scheme != Uri.UriSchemeHttps && link.Scheme != Uri.UriSchemeHttp))
        {
            TempData["Notice"] = "The application link is unavailable. Please check back later.";
            return RedirectToAction(nameof(Details), new { slug = job.Slug });
        }
        if (!IsAdminSession)
            await _context.Jobs.Where(j => j.Id == id).ExecuteUpdateAsync(set => set.SetProperty(j => j.ApplyClicks, j => j.ApplyClicks + 1));
        return Redirect(link.AbsoluteUri);
    }

    private List<int> ReadSavedIds()
    {
        if (!Request.Cookies.TryGetValue(SavedCookie, out var cookie)) return new();
        try { return (JsonSerializer.Deserialize<List<int>>(_savedJobsProtector.Unprotect(cookie)) ?? new()).Where(id => id > 0).Distinct().Take(80).ToList(); }
        catch (Exception exception) when (exception is CryptographicException or JsonException) { return new(); }
    }
    private void WriteSavedIds(List<int> ids) => Response.Cookies.Append(SavedCookie, _savedJobsProtector.Protect(JsonSerializer.Serialize(ids)), new CookieOptions
    {
        HttpOnly = true, Secure = Request.IsHttps, SameSite = SameSiteMode.Lax, IsEssential = true, MaxAge = TimeSpan.FromDays(180), Path = "/"
    });
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveJob(int id)
    {
        var job = await AvailableJobs().FirstOrDefaultAsync(j => j.Id == id);
        if (job == null) return MissingJob();
        var ids = ReadSavedIds();
        if (!ids.Contains(id))
        {
            if (ids.Count >= 80) { TempData["Notice"] = "You have saved 80 jobs. Remove a saved job before adding another."; }
            else { ids.Add(id); WriteSavedIds(ids); TempData["Notice"] = "Job saved to this browser."; }
        }
        return RedirectToAction(nameof(Details), new { slug = job.Slug });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult RemoveSavedJob(int id)
    {
        var ids = ReadSavedIds(); ids.Remove(id); WriteSavedIds(ids);
        return RedirectToAction(nameof(SavedJobs));
    }
    public async Task<IActionResult> SavedJobs()
    {
        var ids = ReadSavedIds();
        return View(await _context.Jobs.AsNoTracking().Where(j => j.IsActive && ids.Contains(j.Id)).OrderByDescending(j => j.PostedDate).ToListAsync());
    }
    public IActionResult About() => View();
    public IActionResult Privacy() => RedirectToAction(nameof(PrivacyPolicy));
    public IActionResult PrivacyPolicy() => View();
    public IActionResult Terms() => View();
    public IActionResult Disclaimer() => View();
    public IActionResult Contact() => View();
    public IActionResult Advertise() => View();
    [HttpGet("/ads.txt")]
    public IActionResult AdsTxt([FromServices] Microsoft.Extensions.Options.IOptions<AdvertisingOptions> settings)
    {
        var ads = settings.Value;
        return ads.HasPublisher
            ? Content($"google.com, {ads.PublisherId[3..]}, DIRECT, f08c47fec0942fa0\n", "text/plain")
            : NotFound();
    }
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    public async Task<IActionResult> SearchSuggestions(string? term)
    {
        if (string.IsNullOrWhiteSpace(term)) return Json(Array.Empty<string>());
        term = term.Trim();
        if (term.Length > 150) term = term[..150];
        return Json(await AvailableJobs().Where(j => EF.Functions.Like(j.Title, $"%{term}%") || EF.Functions.Like(j.CompanyName, $"%{term}%") || EF.Functions.Like(j.Skills, $"%{term}%")).Select(j => j.Title.Trim()).Distinct().OrderBy(title => title).Take(8).ToListAsync());
    }
    [HttpGet("/robots.txt")]
    public IActionResult Robots() => Content($"User-agent: *\nAllow: /\nDisallow: /Admin\nDisallow: /Analytics\nDisallow: /Settings\nDisallow: /App_Data\nSitemap: {Origin}/sitemap.xml\n", "text/plain");

    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> Sitemap()
    {
        var jobs = await AvailableJobs().OrderByDescending(j => j.PostedDate).Select(j => new { j.Slug, j.PostedDate }).ToListAsync();
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var origin = Origin;
        var root = new XElement(ns + "urlset");
        foreach (var path in new[] { "/", "/Home/About", "/Home/Contact", "/Home/PrivacyPolicy", "/Home/Terms", "/Home/Disclaimer", "/Home/Advertise", "/job-alerts" })
            root.Add(new XElement(ns + "url", new XElement(ns + "loc", origin + path)));
        foreach (var category in JobCategories.All) root.Add(new XElement(ns + "url", new XElement(ns + "loc", origin + "/jobs/" + JobCategories.Slug(category))));
        foreach (var job in jobs.Where(j => !string.IsNullOrEmpty(j.Slug)))
            root.Add(new XElement(ns + "url", new XElement(ns + "loc", origin + "/job/" + Uri.EscapeDataString(job.Slug!)), new XElement(ns + "lastmod", job.PostedDate.ToString("yyyy-MM-dd"))));
        return Content(new XDocument(new XDeclaration("1.0", "utf-8", null), root).ToString(), "application/xml");
    }
}



