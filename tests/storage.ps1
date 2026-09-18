$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$run = Join-Path $repo ('artifacts\storage-test-' + [Guid]::NewGuid().ToString('N'))
$site = Join-Path $run 'site'
$legacy = Join-Path $site 'wwwroot\uploads\resumes'
$private = Join-Path $site 'App_Data\Resumes'
[IO.Directory]::CreateDirectory($legacy) | Out-Null
[IO.File]::WriteAllText((Join-Path $legacy 'test.pdf'), 'synthetic resume test')
& (Join-Path $repo 'scripts\Move-LegacyResumes.ps1') -SiteRoot $site -WhatIf
if (-not (Test-Path -LiteralPath (Join-Path $legacy 'test.pdf'))) { throw 'WhatIf moved a file.' }
& (Join-Path $repo 'scripts\Move-LegacyResumes.ps1') -SiteRoot $site
if ((Test-Path -LiteralPath (Join-Path $legacy 'test.pdf')) -or -not (Test-Path -LiteralPath (Join-Path $private 'test.pdf'))) { throw 'Migration failed.' }
if ([IO.File]::ReadAllText((Join-Path $private 'test.pdf')) -ne 'synthetic resume test') { throw 'Migration changed content.' }
& (Join-Path $repo 'scripts\Move-LegacyResumes.ps1') -SiteRoot $site
[IO.File]::WriteAllText((Join-Path $legacy 'test.pdf'), 'collision')
$rejected = $false
try { & (Join-Path $repo 'scripts\Move-LegacyResumes.ps1') -SiteRoot $site } catch { $rejected = $true }
if (-not $rejected -or [IO.File]::ReadAllText((Join-Path $private 'test.pdf')) -ne 'synthetic resume test') { throw 'Collision was not safely rejected.' }
$keys = Join-Path $site 'App_Data\Keys'
[IO.Directory]::CreateDirectory($keys) | Out-Null
[IO.File]::WriteAllText((Join-Path $keys '.synthetic-key'), 'synthetic key')
$backup = Join-Path $run 'backup.zip'
& (Join-Path $repo 'scripts\Backup-SiteFiles.ps1') -SiteRoot $site -Destination $backup
$zip = [IO.Compression.ZipFile]::OpenRead($backup)
try {
    $names = @($zip.Entries | ForEach-Object FullName)
    foreach ($expected in @('App_Data/Keys/.synthetic-key','App_Data/Resumes/test.pdf','wwwroot/uploads/resumes/test.pdf')) {
        if ($names -notcontains $expected) { throw "Archive missing $expected" }
    }
    $restored = Join-Path $run 'restored'
    [IO.Compression.ZipFileExtensions]::ExtractToDirectory($zip, $restored)
    if ([IO.File]::ReadAllText((Join-Path $restored 'App_Data\Resumes\test.pdf')) -ne 'synthetic resume test') { throw 'Restored content mismatch.' }
} finally { $zip.Dispose() }
$rejected = $false
try { & (Join-Path $repo 'scripts\Backup-SiteFiles.ps1') -SiteRoot $site -Destination $backup } catch { $rejected = $true }
if (-not $rejected) { throw 'Existing backup was overwritten.' }
$rejected = $false
try { & (Join-Path $repo 'scripts\Backup-SiteFiles.ps1') -SiteRoot $site -Destination (Join-Path $site 'public.zip') } catch { $rejected = $true }
if (-not $rejected) { throw 'Backup inside site was allowed.' }
Write-Output 'PASS: migration preview, move, content, rerun, collision rejection, backup entries, restore, overwrite rejection and destination restriction.'
