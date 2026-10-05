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
        .Where(j => j.IsActive && (j.AvailableFrom == null || j.AvailableFrom <= DateTime.Today) && (j.ExpiryDate == null || j.ExpiryDate >= DateTime.Today) && (j.SourcePostedDate ?? j.PostedDate).Date > DateTime.Today.AddDays(-30));
    private IQueryable<Job> DiscoverableJobs() => AvailableJobs();
    private static IQueryable<Job> CategoryJobs(IQueryable<Job> query, string category) => category switch
    {
        "Off Campus" => query.Where(j => j.Category == "Off Campus" || j.SubCategory == "Off Campus Drive"),
        "Walk-in" => query.Where(j => j.Category == "Walk-in" || j.SubCategory == "Walk-in Drive"),
        "Internship Programs" or "Internship" => query.Where(j => j.Category == "Internship Programs" || j.Category == "Internship"),
        "Work From Home Jobs" or "WFH Jobs" => query.Where(j => j.Category == "Work From Home Jobs" || j.Category == "WFH Jobs" || j.JobType == "Remote"),
        _ => query.Where(j => j.Category == category)
    };

    private static IQueryable<Job> ApplyDiscoveryFilters(IQueryable<Job> query, string? search, string? category,
        string? location, string? experience, string? qualification, int? batch)
    {
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(j => EF.Functions.Like(j.Title, $"%{search}%") || EF.Functions.Like(j.CompanyName, $"%{search}%") || EF.Functions.Like(j.Location, $"%{search}%") || EF.Functions.Like(j.Skills, $"%{search}%"));
        if (!string.IsNullOrWhiteSpace(category)) query = CategoryJobs(query, category);
        if (!string.IsNullOrWhiteSpace(location)) query = query.Where(j => EF.Functions.Like(j.Location, $"%{location}%"));
        if (!string.IsNullOrWhiteSpace(experience)) query = query.Where(j => EF.Functions.Like(j.Experience, $"%{experience}%"));
        if (!string.IsNullOrWhiteSpace(qualification)) query = query.Where(j => j.Qualification.Contains(qualification));
        if (batch.HasValue) query = query.Where(j => j.BatchFrom <= batch.Value && j.BatchTo >= batch.Value);
        return query;
    }

    [HttpGet("/jobs", Name = "all-jobs")]
    public Task<IActionResult> AllJobs(string? search, string? category, string? location, string? experience, string? qualification, int page = 1, string sort = "latest", int? batch = null)
    {
        ViewData["AllJobs"] = true;
        return Index(search, category, location, experience, qualification, page, sort, batch);
    }

    public async Task<IActionResult> Index(string? search, string? category, string? location, string? experience, string? qualification, int page = 1, string sort = "latest", int? batch = null)
    {
        const int pageSize = 12;
        search = search?.Trim(); location = location?.Trim(); qualification = qualification?.Trim();
        category = category switch { "Internship" => "Internship Programs", "WFH Jobs" => "Work From Home Jobs", _ => category };
        if (batch.HasValue && (batch < 1990 || batch > 2100)) return BadRequest("Choose a graduation year between 1990 and 2100.");
        var categorySlug = JobCategories.Slug(category);
        if (!string.IsNullOrEmpty(category) && categorySlug == null) return MissingJob();
        if (categorySlug != null && Request.Path != "/jobs/" + categorySlug)
            return RedirectToRoutePermanent("category", new { categorySlug, search, location, experience, qualification, page, sort, batch });
        if (categorySlug == null && ViewData["AllJobs"] is not true &&
            (page != 1 || sort != "latest" || batch.HasValue || !string.IsNullOrWhiteSpace(search + location + experience + qualification)))
            return RedirectToRoute("all-jobs", new { search, location, experience, qualification, page, sort, batch });
        ViewBag.Batch = batch;
        ViewData["Title"] = string.IsNullOrEmpty(category)
            ? "Latest Fresher Jobs, Off-Campus Drives & Internships in India | D2DJobs"
            : $"{category} for Freshers in India | Latest Openings | D2DJobs";
        ViewData["MetaDescription"] = category switch
        {
            "Off Campus" => "Find current off-campus drives for freshers in India. Review eligibility, graduation batches, deadlines and official application links.",
            "Walk-in" => "Browse current walk-in interviews in India. Check job locations, eligibility, interview dates and employer application details.",
            "Internship Programs" => "Explore internship openings for students and freshers in India. Check qualifications, skills, dates and official application details.",
            "Work From Home Jobs" => "Find remote and work-from-home jobs for freshers in India. Review role requirements, location details and official application links.",
            "Government Jobs" => "Browse government job openings and recruitment updates. Check qualifications, eligibility, important dates and official notices before applying.",
            "Bank Jobs" => "Explore bank job openings for freshers in India. Review qualifications, eligibility and application information from official sources.",
            "IT Jobs" => "Find IT and software jobs for freshers in India. Explore roles, required skills, locations and employer application details.",
            "Core Engineering Jobs" => "Browse core engineering opportunities for freshers in India. Check branches, qualifications, locations and application details.",
            "BPO / Support Jobs" => "Explore BPO and customer support jobs for freshers in India. Review role requirements, locations and official application details.",
            "Freshers Jobs" => "Browse current fresher jobs across India. Check qualifications, skills, locations and application deadlines before applying.",
            _ => "Browse current fresher jobs, off-campus drives and internships in India. Filter openings by graduation batch, qualification and location."
        };
        ViewData["Canonical"] = Origin + (categorySlug != null ? "/jobs/" + categorySlug : ViewData["AllJobs"] is true ? "/jobs" : "/");
        // Let visitors see every currently active, in-date listing. Keep the stricter
        // editorial gate separately for search indexing, rich results and advertising.
        var available = AvailableJobs();
        var jobs = ApplyDiscoveryFilters(available, search, category, location, experience, qualification, batch);
        var publishableJobs = ApplyDiscoveryFilters(DiscoverableJobs(), search, category, location, experience, qualification, batch);
        var total = await jobs.CountAsync();
        var publishableTotal = await publishableJobs.CountAsync();
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
        var resultIds = result.Select(job => job.Id).ToArray();
        var publishableOnPage = resultIds.Length == 0 ? 0 : await publishableJobs.CountAsync(job => resultIds.Contains(job.Id));
        var allResultsPublishable = result.Count > 0 && publishableOnPage == result.Count;
        ViewData["ShowAds"] = allResultsPublishable;
        ViewData["NoIndex"] = total == 0 || publishableTotal != total;
        ViewBag.Search = search; ViewBag.Category = category; ViewBag.Location = location; ViewBag.Experience = experience; ViewBag.Sort = sort;
        ViewBag.Qualification = qualification;
        var totals = await available.GroupBy(j => 1).Select(g => new {
            Freshers = g.Count(j => j.Category == "Freshers Jobs"),
            OffCampus = g.Count(j => j.Category == "Off Campus" || j.SubCategory == "Off Campus Drive"),
            WalkIn = g.Count(j => j.Category == "Walk-in" || j.SubCategory == "Walk-in Drive"),
            IT = g.Count(j => j.Category == "IT Jobs"), Government = g.Count(j => j.Category == "Government Jobs"),
            Bank = g.Count(j => j.Category == "Bank Jobs"),
            Internship = g.Count(j => j.Category == "Internship Programs" || j.Category == "Internship"),
            Remote = g.Count(j => j.Category == "Work From Home Jobs" || j.Category == "WFH Jobs" || j.JobType == "Remote"),
            Core = g.Count(j => j.Category == "Core Engineering Jobs"), Bpo = g.Count(j => j.Category == "BPO / Support Jobs")
        }).FirstOrDefaultAsync();
        // The hero count must describe the same filtered query that produced the displayed cards.
        ViewBag.TotalJobs = total;
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
        var showSections = ViewData["AllJobs"] is not true && page == 1 && sort == "latest" && !batch.HasValue && string.IsNullOrWhiteSpace(search + category + location + experience + qualification);
        if (showSections)
            ViewBag.HiringCompanies = await available.Where(j => j.CompanyName != "")
                .GroupBy(j => j.CompanyName).OrderByDescending(g => g.Count()).ThenBy(g => g.Key)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .Take(12).ToDictionaryAsync(company => company.Name, company => company.Count);
        foreach (var name in JobCategories.All)
        {
            var categoryQuery = CategoryJobs(available, name);

            if (showSections && counts[name] > 0)
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
        var jobs = DiscoverableJobs();
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
        var matches = await DiscoverableJobs()
            .Where(j => j.CompanyName == company && j.Title == title && j.Slug != null && j.Slug != "")
            .Select(j => j.Slug).Take(2).ToListAsync();
        // Never choose an unrelated or ambiguous listing across databases.
        return matches.Count == 1
            ? RedirectToRoute("job-details", new { slug = matches[0] })
            : MissingJob();
    }

    public async Task<IActionResult> Details(string slug)
    {
        var job = await _context.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.IsActive && j.Slug == slug);
        if (job == null) return MissingJob();
        if (job.AvailableFrom > DateTime.Today) return MissingJob();
        var expired = job.IsExpired(DateTime.Today);
        if (!IsAdminSession && !expired)
        {
            await _context.Jobs.Where(j => j.Id == job.Id).ExecuteUpdateAsync(set => set.SetProperty(j => j.ViewsCount, j => j.ViewsCount + 1));
            job.ViewsCount++;
        }
        ViewData["Canonical"] = Origin + Url.RouteUrl("job-details", new { slug = job.Slug });
        ViewData["NoIndex"] = expired;
        ViewData["ShowAds"] = !expired;
        ViewBag.IsExpired = expired;
        ViewBag.RelatedJobs = await DiscoverableJobs().Where(j => j.Category == job.Category && j.Id != job.Id).OrderByDescending(j => j.PostedDate).Take(4).ToListAsync();
        ViewBag.IsSaved = ReadSavedIds().Contains(job.Id);
        ViewData["Title"] = $"{job.Title} at {job.CompanyName} | D2DJobs";
        var jobDescription = $"{job.Title} at {job.CompanyName} in {job.Location}. Check {job.Qualification} eligibility, skills and application details.";
        ViewData["MetaDescription"] = jobDescription.Length <= 160 ? jobDescription : jobDescription[..157].TrimEnd() + "...";
        if (expired) Response.StatusCode = StatusCodes.Status410Gone;
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
        return View(await AvailableJobs().Where(j => ids.Contains(j.Id)).OrderByDescending(j => j.PostedDate).ToListAsync());
    }
    public IActionResult About() => View();
    [HttpGet("/editorial-policy")]
    public IActionResult EditorialPolicy() => View();
    [HttpGet("/career-guide")]
    public IActionResult CareerGuide() => View();
    [HttpGet("/resources")]
    public IActionResult Resources()
    {
        ViewData["Title"] = "Career Resources for Freshers | D2DJobs";
        ViewData["MetaDescription"] = "Original, practical guides for fresher resumes, interviews, aptitude tests, walk-ins, safe applications, and avoiding recruitment scams.";
        ViewData["Canonical"] = Origin + "/resources";
        return View(CareerResources.All);
    }
    [HttpGet("/resources/{slug}", Name = "career-resource")]
    public IActionResult Resource(string slug)
    {
        var article = CareerResources.Find(slug);
        if (article == null) return MissingJob();
        ViewData["Title"] = article.Title + " | D2DJobs";
        ViewData["MetaDescription"] = article.Summary;
        ViewData["Canonical"] = Origin + "/resources/" + Uri.EscapeDataString(article.Slug);
        return View(article);
    }
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
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult StatusCodePage(int code)
    {
        Response.StatusCode = code is >= 400 and <= 599 ? code : StatusCodes.Status500InternalServerError;
        return Response.StatusCode == StatusCodes.Status404NotFound
            ? View("NotFound")
            : View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public async Task<IActionResult> SearchSuggestions(string? term)
    {
        if (string.IsNullOrWhiteSpace(term)) return Json(Array.Empty<string>());
        term = term.Trim();
        if (term.Length > 150) term = term[..150];
        return Json(await DiscoverableJobs().Where(j => EF.Functions.Like(j.Title, $"%{term}%") || EF.Functions.Like(j.CompanyName, $"%{term}%") || EF.Functions.Like(j.Skills, $"%{term}%")).Select(j => j.Title.Trim()).Distinct().OrderBy(title => title).Take(8).ToListAsync());
    }
    [HttpGet("/robots.txt")]
    public IActionResult Robots() => Content($"User-agent: *\nAllow: /\nDisallow: /Admin\nDisallow: /Analytics\nDisallow: /Settings\nDisallow: /Readiness\nDisallow: /admin\nDisallow: /employer\nDisallow: /App_Data\nSitemap: {Origin}/sitemap.xml\n", "text/plain");

    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> Sitemap()
    {
        var indexable = DiscoverableJobs();
        var jobs = await indexable.OrderByDescending(j => j.PostedDate).Select(j => new { j.Slug, j.PostedDate, j.LastVerifiedUtc }).ToListAsync();
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var origin = Origin;
        var root = new XElement(ns + "urlset");
        var staticPaths = new List<string> { "/jobs", "/Home/About", "/editorial-policy", "/career-guide", "/resources", "/Home/Contact", "/Home/PrivacyPolicy", "/Home/Terms", "/Home/Disclaimer", "/Home/Advertise", "/job-alerts", "/resume-builder" };
        if (jobs.Count > 0) staticPaths.Insert(0, "/");
        foreach (var path in staticPaths)
            root.Add(new XElement(ns + "url", new XElement(ns + "loc", origin + path)));
        foreach (var article in CareerResources.All)
            root.Add(new XElement(ns + "url", new XElement(ns + "loc", origin + "/resources/" + article.Slug), new XElement(ns + "lastmod", article.Updated.ToString("yyyy-MM-dd"))));
        foreach (var category in JobCategories.All)
            if (await CategoryJobs(indexable, category).AnyAsync())
                root.Add(new XElement(ns + "url", new XElement(ns + "loc", origin + "/jobs/" + JobCategories.Slug(category))));
        foreach (var job in jobs.Where(j => !string.IsNullOrEmpty(j.Slug)))
            root.Add(new XElement(ns + "url", new XElement(ns + "loc", origin + "/job/" + Uri.EscapeDataString(job.Slug!)), new XElement(ns + "lastmod", (job.LastVerifiedUtc ?? job.PostedDate).ToString("yyyy-MM-dd"))));
        return Content(new XDocument(new XDeclaration("1.0", "utf-8", null), root).ToString(), "application/xml");
    }
}



