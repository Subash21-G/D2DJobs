[CmdletBinding()]
param([string]$ReleaseName)
$ErrorActionPreference = 'Stop'
Push-Location (Join-Path $PSScriptRoot '..')
try {
    if ([string]::IsNullOrWhiteSpace($ReleaseName)) { $ReleaseName = 'release-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') }
    if ($ReleaseName -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$') { throw 'ReleaseName may contain only letters, digits, dots, underscores and hyphens.' }
    $releaseParent = Join-Path (Get-Location) 'artifacts/releases'
    $releasePath = Join-Path $releaseParent $ReleaseName
    if (Test-Path -LiteralPath $releasePath) { throw "Release output already exists and will not be overwritten: $releasePath" }
    $stagingPath = Join-Path $releaseParent ('.' + $ReleaseName + '-' + [guid]::NewGuid().ToString('N') + '.staging')
    $sqlPath = Join-Path $stagingPath 'deploy.sql'
    $publishPath = Join-Path $stagingPath 'publish'

    dotnet restore JobForFresher.csproj
    if ($LASTEXITCODE -ne 0) { throw 'Restore failed.' }
    dotnet build JobForFresher.csproj -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path (Get-Location) 'tests/Verify-PendingMigration.ps1') -Configuration Release
    if ($LASTEXITCODE -ne 0) { throw 'Pending migration safety verification failed; no release output was generated.' }

    New-Item -ItemType Directory -Path $releaseParent -Force | Out-Null
    New-Item -ItemType Directory -Path $stagingPath -Force | Out-Null
    dotnet ef migrations script --idempotent --no-build --configuration Release --output $sqlPath
    if ($LASTEXITCODE -ne 0) { throw 'Idempotent migration script generation failed.' }
    $sql = [System.IO.File]::ReadAllText($sqlPath)
    if ($sql -notmatch '20261001082905_ActivateImportedJobs') { throw 'Idempotent SQL is missing the preserved pending migration ID.' }
    if ($sql -match '(?is)\bUPDATE\s+(?:\[dbo\]\.\s*)?\[Jobs\].{0,400}\[IsActive\]\s*=\s*(?:1|CAST\s*\(\s*1\s+AS\s+bit\s*\)).{0,400}\[ImportedUtc\]') { throw 'Idempotent SQL contains an operation that can reactivate imported jobs.' }

    dotnet publish JobForFresher.csproj -c Release --no-build --output $publishPath
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
    if (Test-Path -LiteralPath (Join-Path $publishPath 'appsettings.Local.json')) { throw 'Local secrets found in publish output.' }
    if (Test-Path -LiteralPath (Join-Path $publishPath 'App_Data')) { throw 'Private runtime data found in publish output.' }

    New-Item -ItemType Directory -Path $releaseParent -Force | Out-Null
    Move-Item -LiteralPath $stagingPath -Destination $releasePath
    Write-Output "Release files and verified idempotent migration SQL are ready in $releasePath. No database was connected to or updated."
} finally { Pop-Location }
