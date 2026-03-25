param(
    [string]$Target = "C:\inetpub\yonetiq",
    [string]$Config = "Release",
    [switch]$StopSite
)

$ProjectPath = "D:\Dev\yonet\yonetiq\yonetiq.csproj"
$PublishPath  = "D:\Dev\yonet\_publish"

Write-Host "=== YonetIQ Publish ===" -ForegroundColor Cyan

if ($StopSite) {
    Stop-Website -Name "YonetIQ" -ErrorAction SilentlyContinue
    Write-Host "Site durduruldu" -ForegroundColor Yellow
}

dotnet publish $ProjectPath `
    -c $Config `
    -r win-x64 `
    --self-contained false `
    -o $PublishPath `
    --nologo

if ($LASTEXITCODE -ne 0) {
    Write-Host "BUILD HATA!" -ForegroundColor Red
    exit 1
}

$excludeFiles = @("appsettings.Development.json", "*.pdb")

Get-ChildItem $PublishPath -Recurse | Where-Object {
    $name = $_.Name
    -not ($excludeFiles | Where-Object { $name -like $_ })
} | ForEach-Object {
    $dest = $_.FullName.Replace($PublishPath, $Target)
    $destDir = Split-Path $dest -Parent
    if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }
    Copy-Item $_.FullName -Destination $dest -Force
}

Write-Host "Dosyalar kopyalandi: $Target" -ForegroundColor Green

if ($StopSite) {
    Start-Website -Name "YonetIQ"
    Write-Host "Site baslatildi" -ForegroundColor Green
}

Write-Host "=== TAMAMLANDI ===" -ForegroundColor Cyan
