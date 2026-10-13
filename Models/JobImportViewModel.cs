using Microsoft.AspNetCore.Http;

namespace JobForFresher.Models;

public class JobImportViewModel
{
    public IFormFile? File { get; set; }
    public int ImportedCount { get; set; }
    public int RejectedCount { get; set; }
    public int PublishedCount { get; set; }
    public string? Summary { get; set; }
    public List<string> Errors { get; } = new();
}
