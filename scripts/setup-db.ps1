$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$envPath = Join-Path $repoRoot ".env"
$dbDir = Join-Path $repoRoot ".db"

function Load-DotEnv([string]$path) {
    if (-not (Test-Path $path)) { return }

    Get-Content $path | ForEach-Object {
        $line = $_.Trim()
        if (-not $line -or $line.StartsWith("#")) { return }

        $idx = $line.IndexOf("=")
        if ($idx -le 0) { return }

        $key = $line.Substring(0, $idx).Trim()
        $value = $line.Substring($idx + 1).Trim().Trim('"')
        $value = $value.Replace('\\', '\')

        if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($key))) {
            [Environment]::SetEnvironmentVariable($key, $value)
        }
    }
}

Load-DotEnv $envPath

if ([string]::IsNullOrWhiteSpace($env:SQLCLI_CONN) -and -not [string]::IsNullOrWhiteSpace($env:YONET_CONN)) {
    $env:SQLCLI_CONN = $env:YONET_CONN
}

if ([string]::IsNullOrWhiteSpace($env:SQLCLI_CONN)) {
    Write-Error "SQLCLI_CONN bos. .env dosyasina baglanti bilgisi ekleyin."
    exit 1
}

$conn = $env:SQLCLI_CONN
$dbName = "YonetDB"
if ($conn -match '(?i)Database\s*=\s*([^;]+)') {
    $dbName = $matches[1].Trim()
}

New-Item -ItemType Directory -Force $dbDir | Out-Null
$mdfPath = Join-Path $dbDir "$dbName.mdf"
$ldfPath = Join-Path $dbDir "$dbName.ldf"

$mdfSql = $mdfPath.Replace("'", "''")
$ldfSql = $ldfPath.Replace("'", "''")

$masterConn = [regex]::Replace($conn, '(?i)Database\s*=\s*[^;]+', 'Database=master')
if ($masterConn -eq $conn) {
    $masterConn = $conn.TrimEnd(';') + ';Database=master;'
}

$createDbSql = @"
DECLARE @dbName sysname = N'$dbName';
DECLARE @mdf nvarchar(4000) = N'$mdfSql';
DECLARE @ldf nvarchar(4000) = N'$ldfSql';

IF DB_ID(@dbName) IS NOT NULL
BEGIN
    DECLARE @physical nvarchar(4000);
    SELECT TOP(1) @physical = physical_name
    FROM sys.master_files
    WHERE database_id = DB_ID(@dbName) AND type_desc = 'ROWS';

    IF @physical IS NULL OR LOWER(@physical) <> LOWER(@mdf)
    BEGIN
        DECLARE @dropSql nvarchar(max) = N'ALTER DATABASE [' + @dbName + N'] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [' + @dbName + N'];';
        EXEC (@dropSql);
    END
END

IF DB_ID(@dbName) IS NULL
BEGIN
    DECLARE @createSql nvarchar(max) = N'CREATE DATABASE [' + @dbName + N'] ON PRIMARY (NAME = N''' + @dbName + N''', FILENAME = N''' + @mdf + N''') LOG ON (NAME = N''' + @dbName + N'_log'', FILENAME = N''' + @ldf + N''');';
    EXEC (@createSql);
END
"@

Push-Location $repoRoot
try {
    $env:SQLCLI_CONN = $masterConn
    dotnet tool run sqlcli query $createDbSql
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    $env:SQLCLI_CONN = $conn
    dotnet tool run sqlcli migrate
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    dotnet tool run sqlcli seed
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    dotnet tool run sqlcli status
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
