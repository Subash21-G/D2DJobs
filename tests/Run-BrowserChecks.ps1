[CmdletBinding()]
param([string]$PlaywrightModules, [int]$Port = 7233)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
Push-Location $repo
$server = $null
try {
    if (Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue) { throw 'Test port is already in use.' }
    # Use a fresh local database only. Never read production database credentials.
    $database = 'JobForFresher_Regression_' + [Guid]::NewGuid().ToString('N')
    $env:ConnectionStrings__DefaultConnection = "Server=(localdb)\MSSQLLocalDB;Database=$database;Trusted_Connection=True;TrustServerCertificate=True"
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ASPNETCORE_URLS = "https://localhost:$Port"
    $env:AdminSettings__BootstrapEnabled = 'true'
    $env:AdminSettings__UserName = 'regression-admin'
    $env:AdminSettings__Email = 'regression@example.invalid'
    $env:AdminSettings__Password = [Guid]::NewGuid().ToString('N') + '!Test1'
    $env:Advertising__Enabled = 'false'
    $env:TEST_ADMIN_USER = $env:AdminSettings__UserName
    $env:TEST_ADMIN_PASSWORD = $env:AdminSettings__Password
    $env:TEST_ISOLATED_DATABASE = $database
    $env:TEST_BASE_URL = $env:ASPNETCORE_URLS
    if ($PlaywrightModules) { $env:NODE_PATH = (Resolve-Path -LiteralPath $PlaywrightModules).Path }
    New-Item -ItemType Directory -Path artifacts -Force | Out-Null
    $output = Join-Path $repo 'artifacts\regression-build'
    dotnet build JobForFresher.csproj --no-restore -o $output
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    # Dedicated content root isolates persisted security keys and settings.
    $content = Join-Path $repo ('artifacts\' + $database)
    [IO.Directory]::CreateDirectory($content) | Out-Null
    $env:ASPNETCORE_CONTENTROOT = $content
    $env:ASPNETCORE_WEBROOT = Join-Path $repo 'wwwroot'
    $server = Start-Process -FilePath dotnet -ArgumentList @((Join-Path $output 'JobForFresher.dll')) -WorkingDirectory $repo -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $content 'server.log') -RedirectStandardError (Join-Path $content 'error.log')
    $ready = $false
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        if ($server.HasExited) { throw 'Test server exited. Inspect the isolated error log.' }
        try {
            $tcp = New-Object Net.Sockets.TcpClient
            $tcp.Connect('localhost', $Port)
            $tcp.Dispose()
            $ready = $true
            break
        } catch { Start-Sleep -Seconds 1 }
    }
    if (-not $ready) { throw 'Test server did not start.' }
    node tests/browser.cjs
    if ($LASTEXITCODE -ne 0) { throw 'Browser regression failed.' }
    Write-Output "Isolated database retained for inspection: $database"
} finally {
    if ($server -and -not $server.HasExited) { Stop-Process -Id $server.Id }
    Pop-Location
}
