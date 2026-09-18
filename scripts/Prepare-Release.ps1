[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Push-Location (Join-Path $PSScriptRoot '..')
try {
    if (Test-Path -LiteralPath artifacts/publish) { throw 'Use a fresh release workspace or archive the existing artifacts/publish folder first.' }
    dotnet restore JobForFresher.csproj
    if ($LASTEXITCODE -ne 0) { throw 'Restore failed.' }
    dotnet build JobForFresher.csproj -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    New-Item -ItemType Directory -Path artifacts -Force | Out-Null
    dotnet ef migrations script --idempotent --no-build --configuration Release --output artifacts/deploy.sql
    if ($LASTEXITCODE -ne 0) { throw 'Migration script generation failed.' }
    dotnet publish JobForFresher.csproj -c Release --no-build --output artifacts/publish
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
    if (Test-Path -LiteralPath artifacts/publish/appsettings.Local.json) { throw 'Local secrets found in publish output.' }
    if (Test-Path -LiteralPath artifacts/publish/App_Data) { throw 'Private runtime data found in publish output.' }
    Write-Output 'Release files and idempotent migration SQL are ready in artifacts. No database or website was deployed.'
} finally { Pop-Location }
