[CmdletBinding()]
param([string]$PlaywrightModules, [int]$Port = 7233, [string]$TestScript = 'tests/browser.cjs', [switch]$VerifyAdGating, [ValidatePattern('^[A-Za-z0-9-]{1,32}$')][string]$BuildName = 'regression-build')
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
    $env:Advertising__PublisherId = 'ca-pub-0000000000000000'
    $env:Monetag__Enabled = 'false'
    $env:Monetag__SecondaryEnabled = 'false'
    if ($VerifyAdGating) {
        # Fake ad configuration only, inside the isolated test server. The content-quality
        # browser script blocks all non-local requests so no ad requests are sent.
        if ($TestScript -ne 'tests/content-quality.cjs') { throw 'Ad gate verification requires the content-quality test script.' }
        $env:ASPNETCORE_ENVIRONMENT = 'Production'
        $env:Database__ApplyMigrations = 'true'
        $env:Advertising__Enabled = 'true'
        $env:Advertising__SiteApproved = 'true'
        $env:Advertising__ConsentConfigured = 'true'
        $env:Advertising__Slots__ListingInline = '0000000000'
        $env:Advertising__Slots__JobSidebar = '0000000000'
        $env:Monetag__Enabled = 'true'
        $env:Monetag__ZoneId = '00000000'
        $env:Monetag__SecondaryEnabled = 'true'
        $env:Monetag__SecondaryZoneId = '00000001'
    }
    $env:TEST_ADMIN_USER = $env:AdminSettings__UserName
    $env:TEST_ADMIN_PASSWORD = $env:AdminSettings__Password
    $env:TEST_ISOLATED_DATABASE = $database
    $env:TEST_BASE_URL = $env:ASPNETCORE_URLS
    if ($PlaywrightModules) { $env:NODE_PATH = (Resolve-Path -LiteralPath $PlaywrightModules).Path }
    New-Item -ItemType Directory -Path artifacts -Force | Out-Null
    $output = Join-Path $repo ('artifacts\' + $BuildName)
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
    node $TestScript
    if ($LASTEXITCODE -ne 0) { throw 'Browser regression failed.' }
    if ($VerifyAdGating) {
        # Reuse the isolated five-job database to verify deployment overrides.
        Stop-Process -Id $server.Id
        $server.WaitForExit()
        $server = $null
        foreach ($scenario in @(
            @{ Name = 'primary-disabled'; Environment = 'Production'; Primary = 'false'; Secondary = 'true'; PrimaryExpected = $false; SecondaryExpected = $true },
            @{ Name = 'secondary-disabled'; Environment = 'Production'; Primary = 'true'; Secondary = 'false'; PrimaryExpected = $true; SecondaryExpected = $false },
            @{ Name = 'both-disabled'; Environment = 'Production'; Primary = 'false'; Secondary = 'false'; PrimaryExpected = $false; SecondaryExpected = $false },
            @{ Name = 'development'; Environment = 'Development'; Primary = 'true'; Secondary = 'true'; PrimaryExpected = $false; SecondaryExpected = $false }
        )) {
            $env:ASPNETCORE_ENVIRONMENT = $scenario.Environment
            $env:Monetag__Enabled = $scenario.Primary
            $env:Monetag__SecondaryEnabled = $scenario.Secondary
            $server = Start-Process -FilePath dotnet -ArgumentList @((Join-Path $output 'JobForFresher.dll')) -WorkingDirectory $repo -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $content ($scenario.Name + '.log')) -RedirectStandardError (Join-Path $content ($scenario.Name + '.error.log'))
            $html = & curl.exe --insecure --silent --show-error --retry 10 --retry-connrefused --retry-delay 1 --max-time 15 "https://localhost:$Port/jobs"
            if ($LASTEXITCODE -ne 0) { throw "Could not load jobs page for $($scenario.Name)." }
            $hasPrimary = [bool]($html -match 'https://nap5k\.com/tag\.min\.js')
            $hasSecondary = [bool]($html -match 'https://quge5\.com/88/tag\.min\.js')
            if ($hasPrimary -ne $scenario.PrimaryExpected -or $hasSecondary -ne $scenario.SecondaryExpected) {
                throw "Monetag tag gate failed for $($scenario.Name): primary=$hasPrimary secondary=$hasSecondary."
            }
            Stop-Process -Id $server.Id
            $server.WaitForExit()
            $server = $null
        }
        Write-Output 'Monetag enabled-flag and Development gating: 4 scenarios passed.'
    }
    Write-Output "Isolated database retained for inspection: $database"
} finally {
    if ($server -and -not $server.HasExited) { Stop-Process -Id $server.Id }
    Pop-Location
}
