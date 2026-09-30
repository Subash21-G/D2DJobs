$ErrorActionPreference = 'Stop'
# Simulate legacy rows that predate the publishing gate, only in the runner's disposable DB.
$database = $env:TEST_ISOLATED_DATABASE
if ($database -notmatch '^JobForFresher_Regression_[a-f0-9]{32}$') { throw 'Isolated regression database required.' }
$connection = New-Object System.Data.SqlClient.SqlConnection("Server=(localdb)\MSSQLLocalDB;Database=$database;Integrated Security=True;TrustServerCertificate=True")
try {
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = @'
UPDATE Jobs SET IsActive = 1 WHERE Title IN ('QUALITY Duplicate', 'QUALITY Thin', 'QUALITY Stale');
UPDATE Jobs SET AvailableFrom = DATEADD(day, 5, GETDATE()) WHERE Title = 'QUALITY Scheduled';
SELECT Title, Slug, IsActive FROM Jobs WHERE Title LIKE 'QUALITY %';
'@
    $reader = $command.ExecuteReader()
    $rows = @()
    while ($reader.Read()) {
        $rows += [pscustomobject]@{ title = $reader.GetString(0); slug = $reader.GetString(1); active = $reader.GetBoolean(2) }
    }
    $reader.Close()
    ConvertTo-Json -InputObject $rows -Compress
} finally { $connection.Dispose() }
