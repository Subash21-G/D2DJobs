[CmdletBinding(SupportsShouldProcess)]
param([Parameter(Mandatory)][string]$SiteRoot)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $SiteRoot).Path.TrimEnd('\', '/')
$source = [IO.Path]::GetFullPath((Join-Path $root 'wwwroot\uploads\resumes'))
$destination = [IO.Path]::GetFullPath((Join-Path $root 'App_Data\Resumes'))
foreach ($path in @($source, $destination)) {
    if (-not $path.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Storage path escapes site root.' }
}
if ((Get-Item -LiteralPath $root -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'The site root must not be a junction or symlink.' }
# Reject junctions/symlinks in every existing path component.
foreach ($relative in @('wwwroot', 'wwwroot\uploads', 'wwwroot\uploads\resumes', 'App_Data', 'App_Data\Resumes')) {
    $path = Join-Path $root $relative
    if ((Test-Path -LiteralPath $path) -and ((Get-Item -LiteralPath $path -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Storage junctions and symbolic links require manual migration.' }
}
if (-not (Test-Path -LiteralPath $source)) { Write-Output 'No legacy resume folder.'; return }
$entries = @(Get-ChildItem -LiteralPath $source -Force)
if ($entries | Where-Object { $_.PSIsContainer -or ($_.Attributes -band [IO.FileAttributes]::ReparsePoint) }) { throw 'Nested folders or links require manual migration; no files were moved.' }
# Validate the entire batch before moving anything; never overwrite a private file.
foreach ($file in $entries) {
    if (Test-Path -LiteralPath (Join-Path $destination $file.Name)) { throw 'A destination filename already exists; no files were moved.' }
}
$count = 0
foreach ($file in $entries) {
    $target = Join-Path $destination $file.Name
    if ($PSCmdlet.ShouldProcess($file.FullName, 'Move into private resume storage')) {
        [IO.Directory]::CreateDirectory($destination) | Out-Null
        Move-Item -LiteralPath $file.FullName -Destination $target -ErrorAction Stop
        $count++
    }
}
Write-Output "$count resume(s) moved. No existing destination file was overwritten."
