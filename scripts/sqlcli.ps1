param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$CommandArgs
)

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$envPath = Join-Path $repoRoot ".env"

if (Test-Path $envPath) {
    Get-Content $envPath | ForEach-Object {
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

if ([string]::IsNullOrWhiteSpace($env:SQLCLI_CONN) -and -not [string]::IsNullOrWhiteSpace($env:YONET_CONN)) {
    $env:SQLCLI_CONN = $env:YONET_CONN
}

if ([string]::IsNullOrWhiteSpace($env:SQLCLI_CONN)) {
    Write-Error "SQLCLI_CONN bos. .env dosyasina baglanti bilgisi ekleyin."
    exit 1
}

Push-Location $repoRoot
try {
    dotnet tool run sqlcli @CommandArgs
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
