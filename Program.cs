using JobForFresher.Interfaces;
using JobForFresher.Services;
using JobForFresher.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsDevelopment()) builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true);
builder.Configuration.AddJsonFile("App_Data/site-settings.json", optional: true, reloadOnChange: true).AddEnvironmentVariables();
builder.Services.AddControllersWithViews(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);
builder.Services.Configure<AdvertisingOptions>(builder.Configuration.GetSection("Advertising"));
builder.Services.Configure<MonetagOptions>(builder.Configuration.GetSection("Monetag"));

builder.Services.Configure<SiteOptions>(builder.Configuration.GetSection("SiteSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ExcelJobImporter>();
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "Keys"));
builder.Services.AddDataProtection().SetApplicationName("JobForFresher").PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "Keys")));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.LoginPath = "/Admin/Login"; options.AccessDeniedPath = "/Admin/Login";
    options.Cookie.Name = "JobForFresher.Admin"; options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30); options.SlidingExpiration = true;
    options.Events.OnValidatePrincipal = async context =>
    {
        var id = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var stamp = context.Principal?.FindFirstValue("security_stamp");
        var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
        if (!int.TryParse(id, out var adminId) || !await db.AdminUsers.AnyAsync(a => a.Id == adminId && a.SecurityStamp == stamp))
        { context.RejectPrincipal(); await context.HttpContext.SignOutAsync(); }
    };
});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("admin-login", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
    options.OnRejected = async (context, token) => { context.HttpContext.Response.Headers.RetryAfter = "60"; await context.HttpContext.Response.WriteAsync("Too many sign-in attempts. Please wait one minute and try again.", token); };
});
var app = builder.Build();
// Migrations are explicit in production; run the reviewed script before deploying.
if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}
if (builder.Configuration.GetValue<bool>("AdminSettings:BootstrapEnabled"))
{
    using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (!db.AdminUsers.Any())
    {
        var name = builder.Configuration["AdminSettings:UserName"];
        var email = builder.Configuration["AdminSettings:Email"];
        var password = builder.Configuration["AdminSettings:Password"];
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || password.Length < 12)
            throw new InvalidOperationException("Supply a username, email and password of at least 12 characters through secret configuration to bootstrap the first admin.");
        var admin = new AdminUser { UserName = name, Email = email, SecurityStamp = Guid.NewGuid().ToString() };
        admin.PasswordHash = AdminPasswords.Hash(admin, password); db.AdminUsers.Add(admin); db.SaveChanges();
    }
}
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    if (context.Request.Path.StartsWithSegments("/uploads/resumes", StringComparison.OrdinalIgnoreCase) || context.Request.Path.StartsWithSegments("/App_Data", StringComparison.OrdinalIgnoreCase))
    { context.Response.StatusCode = 404; return; }
    await next();
});
app.UseStaticFiles(); app.UseRouting(); app.UseRateLimiter(); app.UseAuthentication(); app.UseAuthorization();
app.UseMiddleware<TrafficTrackingMiddleware>();
app.MapControllerRoute("job-details", "job/{slug}", new { controller = "Home", action = "Details" });
app.MapControllerRoute("sitemap", "sitemap.xml", new { controller = "Home", action = "Sitemap" });
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }));
app.Run();