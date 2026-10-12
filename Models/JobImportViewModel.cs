using Microsoft.AspNetCore.Http;

namespace JobForFresher.Models;

public class JobImportViewModel
{
    public IFormFile? File { get; set; }
    public int ImportedCount { get; set; }
    public int RejectedCount { get; set; }
    public int DraftCount { get; set; }
    public string? Summary { get; set; }
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
}
