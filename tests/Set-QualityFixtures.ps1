$ErrorActionPreference = 'Stop'
# Simulate legacy rows that predate the publishing gate, only in the runner's disposable DB.
$database = $env:TEST_ISOLATED_DATABASE
if ($database -notmatch '^JobForFresher_Regression_[a-f0-9]{32}$') { throw 'Isolated regression database required.' }
$connection = New-Object System.Data.SqlClient.SqlConnection("Server=(localdb)\MSSQLLocalDB;Database=$database;Integrated Security=True;TrustServerCertificate=True")
try {
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = @'
UPDATE Jobs SET IsActive = 1 WHERE Title IN ('QUALITY Duplicate', 'QUALITY Thin', 'QUALITY Stale', 'QUALITY GenericApplyUrl', 'QUALITY Expired');
UPDATE Jobs SET AvailableFrom = DATEADD(day, 5, GETDATE()) WHERE Title = 'QUALITY Scheduled';
SELECT Id, Title, Slug, IsActive FROM Jobs WHERE Title LIKE 'QUALITY %';
'@
    $reader = $command.ExecuteReader()
    $rows = @()
    while ($reader.Read()) {
        $rows += [pscustomobject]@{ id = $reader.GetInt32(0); title = $reader.GetString(1); slug = $reader.GetString(2); active = $reader.GetBoolean(3) }
    }
    $reader.Close()
    ConvertTo-Json -InputObject $rows -Compress
} finally { $connection.Dispose() }
