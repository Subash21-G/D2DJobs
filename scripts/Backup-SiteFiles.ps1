[CmdletBinding()]
param([Parameter(Mandatory)][string]$SiteRoot, [Parameter(Mandatory)][string]$Destination)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $SiteRoot).Path.TrimEnd('\', '/')
$target = [IO.Path]::GetFullPath($Destination)
if ($target.Equals($root, [StringComparison]::OrdinalIgnoreCase) -or $target.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Store the backup outside the website directory.' }
if ([IO.Path]::GetExtension($target) -ne '.zip') { throw 'Destination must be a new .zip file.' }
if (Test-Path -LiteralPath $target) { throw 'Backup destination already exists.' }
foreach ($path in @($root, (Join-Path $root 'wwwroot'))) {
    if ((Test-Path -LiteralPath $path) -and ((Get-Item -LiteralPath $path -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Backup source ancestors must not be junctions or symlinks.' }
}
$paths = @('App_Data', 'wwwroot\uploads') | ForEach-Object { Join-Path $root $_ } | Where-Object { Test-Path -LiteralPath $_ }
if (-not $paths) { throw 'No application data or uploads found.' }
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$files = @()
foreach ($path in $paths) {
    $items = @((Get-Item -LiteralPath $path -Force)) + @(Get-ChildItem -LiteralPath $path -Recurse -Force)
    if ($items | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }) { throw 'Backup source contains a junction or symlink; use the host backup system.' }
    $files += $items | Where-Object { -not $_.PSIsContainer }
}
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target)) | Out-Null
$archive = [IO.Compression.ZipFile]::Open($target, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in $files) {
        $entry = $file.FullName.Substring($root.Length + 1).Replace('\', '/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $entry) | Out-Null
    }
} finally { $archive.Dispose() }
$hash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
Write-Output "Site files backed up. SHA256: $hash"
Write-Output 'This does not back up SQL Server or environment secrets. Protect the archive: it contains private documents and security keys.'
