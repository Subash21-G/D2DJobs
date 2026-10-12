[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration = 'Debug')
$ErrorActionPreference = 'Stop'
$migrationId = '20261001082905_ActivateImportedJobs'
$lastAppliedId = '20260930140559_AddJobSourceMetadataAndWalkInRange'
$migrationPath = Join-Path $PSScriptRoot "..\Migrations\$migrationId.cs"
$designerPath = Join-Path $PSScriptRoot "..\Migrations\$migrationId.Designer.cs"
if (-not (Test-Path -LiteralPath $migrationPath) -or -not (Test-Path -LiteralPath $designerPath)) { throw "Migration $migrationId or its designer is missing." }
$migrationAttribute = '[Migration("' + $migrationId + '")]'
if (-not (Get-Content -LiteralPath $designerPath -Raw).Contains($migrationAttribute)) { throw 'The pending migration ID changed.' }

$originalEnvironment = $env:ASPNETCORE_ENVIRONMENT
$originalApplyMigrations = $env:Database__ApplyMigrations
$originalConnection = $env:ConnectionStrings__DefaultConnection
$sqlPath = Join-Path $env:TEMP ("D2DJobs-pending-migration-" + [guid]::NewGuid().ToString('N') + '.sql')
try {
    # This is design-time SQL generation only. Use a deliberately non-production connection string,
    # and keep automatic runtime migration disabled for the child process.
    $env:ASPNETCORE_ENVIRONMENT = 'Production'
    $env:Database__ApplyMigrations = 'false'
    $env:ConnectionStrings__DefaultConnection = 'Server=(localdb)\MSSQLLocalDB;Database=D2DJobs_ScriptGeneration_Only;Trusted_Connection=True;TrustServerCertificate=True'
    dotnet ef migrations script $lastAppliedId $migrationId --no-build --configuration $Configuration --output $sqlPath
    if ($LASTEXITCODE -ne 0) { throw 'EF Core could not generate the pending migration SQL.' }
    $sql = [System.IO.File]::ReadAllText($sqlPath)
    if ($sql -notmatch [regex]::Escape($migrationId)) { throw 'Generated SQL does not preserve the pending migration ID.' }
    if ($sql -match '(?is)\b(?:UPDATE|INSERT|DELETE|ALTER|DROP)\s+(?:\[dbo\]\.\s*)?\[Jobs\](?=\s|;|$)') { throw 'Generated migration SQL contains a Jobs data or schema mutation.' }
    if ($sql -match '(?is)\[ImportedUtc\].{0,300}\[IsActive\].{0,100}=\s*(?:1|CAST\s*\(\s*1\s+AS\s+bit\s*\))') { throw 'Generated migration SQL can reactivate imported jobs.' }
    Write-Output "PASS: $migrationId retains its ID and generates no Jobs operations; imported jobs cannot be reactivated by this migration."
} finally {
    if (Test-Path -LiteralPath $sqlPath) { Remove-Item -LiteralPath $sqlPath -Force }
    if ($null -eq $originalEnvironment) { Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue } else { $env:ASPNETCORE_ENVIRONMENT = $originalEnvironment }
    if ($null -eq $originalApplyMigrations) { Remove-Item Env:Database__ApplyMigrations -ErrorAction SilentlyContinue } else { $env:Database__ApplyMigrations = $originalApplyMigrations }
    if ($null -eq $originalConnection) { Remove-Item Env:ConnectionStrings__DefaultConnection -ErrorAction SilentlyContinue } else { $env:ConnectionStrings__DefaultConnection = $originalConnection }
}
