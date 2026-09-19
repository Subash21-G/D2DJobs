using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using JobForFresher.Models;

namespace JobForFresher.Services;

public sealed class ExcelJobImporter
{
    private static readonly Dictionary<string, string> HeaderAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["title"] = nameof(Job.Title), ["jobtitle"] = nameof(Job.Title), ["job title"] = nameof(Job.Title),
        ["company"] = nameof(Job.CompanyName), ["companyname"] = nameof(Job.CompanyName), ["company name"] = nameof(Job.CompanyName),
        ["category"] = nameof(Job.Category), ["subcategory"] = nameof(Job.SubCategory), ["sub category"] = nameof(Job.SubCategory),
        ["role"] = nameof(Job.Role), ["location"] = nameof(Job.Location), ["experience"] = nameof(Job.Experience),
        ["salary"] = nameof(Job.Salary), ["jobtype"] = nameof(Job.JobType), ["job type"] = nameof(Job.JobType),
        ["qualification"] = nameof(Job.Qualification), ["skills"] = nameof(Job.Skills), ["description"] = nameof(Job.Description),
        ["applylink"] = nameof(Job.ApplyLink), ["apply link"] = nameof(Job.ApplyLink), ["application link"] = nameof(Job.ApplyLink),
        ["isfeatured"] = nameof(Job.IsFeatured), ["featured"] = nameof(Job.IsFeatured),
        ["expirydate"] = nameof(Job.ExpiryDate), ["expiry date"] = nameof(Job.ExpiryDate),
        ["isactive"] = nameof(Job.IsActive), ["active"] = nameof(Job.IsActive),
        ["batchfrom"] = nameof(Job.BatchFrom), ["batch from"] = nameof(Job.BatchFrom),
        ["batchto"] = nameof(Job.BatchTo), ["batch to"] = nameof(Job.BatchTo),
        ["eligibility"] = nameof(Job.Eligibility), ["selectionprocess"] = nameof(Job.SelectionProcess),
        ["selection process"] = nameof(Job.SelectionProcess), ["walkindate"] = nameof(Job.WalkInDate),
        ["walk in date"] = nameof(Job.WalkInDate), ["walkinvenue"] = nameof(Job.WalkInVenue),
        ["walk in venue"] = nameof(Job.WalkInVenue), ["officialsourceurl"] = nameof(Job.OfficialSourceUrl),
        ["official source url"] = nameof(Job.OfficialSourceUrl), ["lastverifiedutc"] = nameof(Job.LastVerifiedUtc),
        ["last verified"] = nameof(Job.LastVerifiedUtc)
    };

    public List<Job> Read(Stream input, List<string> errors)
    {
        using var archive = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen: false);
        var shared = ReadSharedStrings(archive);
        var sheet = archive.GetEntry("xl/worksheets/sheet1.xml");
        if (sheet == null) { errors.Add("The workbook does not contain the first worksheet."); return []; }

        using var sheetStream = sheet.Open();
        var document = XDocument.Load(sheetStream);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var rows = document.Descendants(ns + "row").ToList();
        if (rows.Count == 0) { errors.Add("The first worksheet is empty."); return []; }

        var headers = new Dictionary<int, string>();
        foreach (var cell in rows[0].Elements(ns + "c"))
        {
            var value = CellValue(cell, shared, ns);
            if (!string.IsNullOrWhiteSpace(value))
            {
                var key = Normalize(value);
                if (HeaderAliases.TryGetValue(key, out var property)) headers[ColumnIndex((string?)cell.Attribute("r"))] = property;
            }
        }
        if (!headers.Values.Contains(nameof(Job.Title))) { errors.Add("The first row must contain a Title column."); return []; }

        var jobs = new List<Job>();
        for (var rowNumber = 1; rowNumber < rows.Count; rowNumber++)
        {
            var row = rows[rowNumber];
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in row.Elements(ns + "c"))
            {
                var index = ColumnIndex((string?)cell.Attribute("r"));
                if (headers.TryGetValue(index, out var property)) values[property] = CellValue(cell, shared, ns).Trim();
            }
            if (values.Count == 0 || values.Values.All(string.IsNullOrWhiteSpace)) continue;

            var job = BuildJob(values, rowNumber + 1, errors);
            if (job != null) jobs.Add(job);
        }
        return jobs;
    }

    private static Job? BuildJob(Dictionary<string, string> values, int rowNumber, List<string> errors)
    {
        var job = new Job
        {
            Title = Get(values, nameof(Job.Title)),
            CompanyName = Get(values, nameof(Job.CompanyName)),
            Category = Get(values, nameof(Job.Category)),
            SubCategory = Get(values, nameof(Job.SubCategory)),
            Role = Get(values, nameof(Job.Role)),
            Location = Get(values, nameof(Job.Location)),
            Experience = Get(values, nameof(Job.Experience)),
            Salary = Get(values, nameof(Job.Salary)),
            JobType = Get(values, nameof(Job.JobType)),
            Qualification = Get(values, nameof(Job.Qualification)),
            Skills = Get(values, nameof(Job.Skills)),
            Description = Get(values, nameof(Job.Description)),
            ApplyLink = Get(values, nameof(Job.ApplyLink)),
            Eligibility = Get(values, nameof(Job.Eligibility)),
            SelectionProcess = Get(values, nameof(Job.SelectionProcess)),
            WalkInVenue = Get(values, nameof(Job.WalkInVenue)),
            OfficialSourceUrl = Get(values, nameof(Job.OfficialSourceUrl)),
            IsActive = ParseBool(values, nameof(Job.IsActive), true, rowNumber, errors),
            IsFeatured = ParseBool(values, nameof(Job.IsFeatured), false, rowNumber, errors)
        };
        if (TryInt(values, nameof(Job.BatchFrom), rowNumber, errors, out var batchFrom)) job.BatchFrom = batchFrom;
        if (TryInt(values, nameof(Job.BatchTo), rowNumber, errors, out var batchTo)) job.BatchTo = batchTo;
        if (TryDate(values, nameof(Job.ExpiryDate), rowNumber, errors, out var expiry)) job.ExpiryDate = expiry;
        if (TryDate(values, nameof(Job.WalkInDate), rowNumber, errors, out var walkIn)) job.WalkInDate = walkIn;
        if (TryDate(values, nameof(Job.LastVerifiedUtc), rowNumber, errors, out var verified)) job.LastVerifiedUtc = verified?.ToUniversalTime();

        var validation = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(job, new(job), validation, true))
            foreach (var item in validation) errors.Add($"Row {rowNumber}: {string.Join("; ", item.ErrorMessage ?? "Invalid value.")}");
        return errors.Any(e => e.StartsWith($"Row {rowNumber}:", StringComparison.Ordinal)) ? null : job;
    }

    private static string Get(Dictionary<string, string> values, string key) => values.TryGetValue(key, out var value) ? value : "";
    private static bool ParseBool(Dictionary<string, string> values, string key, bool fallback, int row, List<string> errors)
    {
        var value = Get(values, key);
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        if (bool.TryParse(value, out var result)) return result;
        if (value is "1" or "yes" or "y" or "on") return true;
        if (value is "0" or "no" or "n" or "off") return false;
        errors.Add($"Row {row}: {key} must be true/false, yes/no, or 1/0."); return fallback;
    }
    private static bool TryInt(Dictionary<string, string> values, string key, int row, List<string> errors, out int? result)
    {
        result = null; var value = Get(values, key); if (string.IsNullOrWhiteSpace(value)) return true;
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)) { result = parsed; return true; }
        errors.Add($"Row {row}: {key} must be a whole number."); return false;
    }
    private static bool TryDate(Dictionary<string, string> values, string key, int row, List<string> errors, out DateTime? result)
    {
        result = null; var value = Get(values, key); if (string.IsNullOrWhiteSpace(value)) return true;
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial) && serial > 0 && serial < 100000) { result = DateTime.FromOADate(serial); return true; }
        if (DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out var parsed) ||
            DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out parsed))
        { result = parsed; return true; }
        errors.Add($"Row {row}: {key} must be a valid date."); return false;
    }
    private static string Normalize(string value) => value.Trim().ToLowerInvariant().Replace("_", " ");
    private static int ColumnIndex(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference)) return -1;
        var index = 0;
        foreach (var c in reference.TakeWhile(char.IsLetter)) index = index * 26 + (char.ToUpperInvariant(c) - 'A' + 1);
        return index - 1;
    }
    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml"); if (entry == null) return [];
        using var stream = entry.Open(); var doc = XDocument.Load(stream);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        return doc.Descendants(ns + "si").Select(si => string.Concat(si.Descendants(ns + "t").Select(t => t.Value))).ToList();
    }
    private static string CellValue(XElement cell, List<string> shared, XNamespace ns)
    {
        var type = (string?)cell.Attribute("t"); var value = cell.Element(ns + "v")?.Value ?? "";
        if (type == "s" && int.TryParse(value, out var index) && index >= 0 && index < shared.Count) return shared[index];
        if (type == "inlineStr") return string.Concat(cell.Descendants(ns + "t").Select(t => t.Value));
        return value;
    }
}