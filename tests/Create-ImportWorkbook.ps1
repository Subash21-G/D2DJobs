[CmdletBinding()]
param([Parameter(Mandatory)][string]$OutputPath)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$headers = @('Title','CompanyName','Category','SourceType','Role','Location','Experience','Salary','JobType','Qualification','Skills','Description','ApplyLink','Eligibility','SelectionProcess','ApplicationInstructions','EditorialNote','OfficialSourceUrl','LastVerifiedUtc','PostedDate')
$unsupported = @('QUALITY Imported Unsupported','Imported Test Employer','Company Wise Jobs','OfficialEmployer','Graduate Engineer','Chennai','Fresher','Not disclosed','Full time','Bachelor degree in engineering','Engineering, analysis and communication skills','A test listing used only by the isolated importer regression. This row represents an unsupported classification and must remain private until an administrator chooses one of the approved categories. It includes enough descriptive text for the importer test, but no actual employer or vacancy.','https://example.invalid/unsupported-apply','Engineering graduates with a completed degree and the ability to explain a relevant academic project.','A structured technical exercise followed by a discussion of the submitted approach with the recruitment team.','For this unsupported fixture only, open the linked test source, identify the imported test role, attach the requested resume and keep the confirmation.','This test fixture is intentionally not eligible for publication. Its category is unsupported and it exists only to verify the import review gate.','https://example.invalid/unsupported-source','2026-09-29T00:00:00','2026-09-25T00:00:00')
$supportedAlias = @('QUALITY Imported Date Test','Imported Test Employer','Internship','JobPortal','Graduate QA Engineer','Chennai','Fresher','Not disclosed','Internship','Bachelor degree in engineering','Manual testing, bug reports, SQL and test documentation','This isolated import regression describes a graduate quality assurance internship. The work includes reviewing a small web application, writing repeatable test cases, documenting defects with clear steps, checking fixes, and discussing results with a supervising engineer. The vacancy exists only in the disposable test database and does not describe a real company or opportunity.','https://example.invalid/import-apply','Engineering graduates in the stated eligible batches with a completed degree and the ability to explain a practical project.','The published process for this isolated test is a short technical assessment followed by a structured conversation about test design and defect reporting.','For this imported test opening, choose the graduate QA internship from the listed source, complete its application form, attach the requested resume and keep the portal confirmation for your records.','For this specific test internship, prepare to describe how you reproduced a defect, recorded the steps clearly, checked a proposed fix and communicated the result to a teammate.','https://example.invalid/import-source','2026-09-29T00:00:00','2026-09-25T00:00:00')
$rows = @($headers, $unsupported, $supportedAlias)
function Get-ExcelColumn([int]$Index) {
    $name = ''
    while ($Index -gt 0) { $Index--; $name = [char](65 + ($Index % 26)) + $name; $Index = [math]::Floor($Index / 26) }
    return $name
}
$xmlRows = for ($rowIndex = 0; $rowIndex -lt $rows.Count; $rowIndex++) {
    $cells = for ($columnIndex = 0; $columnIndex -lt $headers.Count; $columnIndex++) {
        $value = [Security.SecurityElement]::Escape([string]$rows[$rowIndex][$columnIndex])
        $address = (Get-ExcelColumn ($columnIndex + 1)) + ($rowIndex + 1)
        "<c r=`"$address`" t=`"inlineStr`"><is><t xml:space=`"preserve`">$value</t></is></c>"
    }
    "<row r=`"$($rowIndex + 1)`">$($cells -join '')</row>"
}
$sheetXml = "<?xml version=`"1.0`" encoding=`"UTF-8`"?><worksheet xmlns=`"http://schemas.openxmlformats.org/spreadsheetml/2006/main`"><sheetData>$($xmlRows -join '')</sheetData></worksheet>"
$absolutePath = [System.IO.Path]::GetFullPath($OutputPath)
[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($absolutePath)) | Out-Null
$file = [System.IO.File]::Open($absolutePath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
$archive = [System.IO.Compression.ZipArchive]::new($file, [System.IO.Compression.ZipArchiveMode]::Create, $false)
try {
    $entry = $archive.CreateEntry('xl/worksheets/sheet1.xml')
    $writer = [System.IO.StreamWriter]::new($entry.Open(), [System.Text.UTF8Encoding]::new($false))
    try { $writer.Write($sheetXml) } finally { $writer.Dispose() }
} finally { $archive.Dispose() }
