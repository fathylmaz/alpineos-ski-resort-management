# Creates an upload archive of the *local, currently working* AlpineOS project.
# This script NEVER pushes code to GitHub and NEVER deletes or changes project files.
param(
  [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
)
$ErrorActionPreference = 'Stop'
$ProjectRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path.TrimEnd('\', '/')
$target = Join-Path (Split-Path $ProjectRoot -Parent) 'AlpineOS_Guncel_Kaynak_ve_Gorseller.zip'
$temp = Join-Path ([System.IO.Path]::GetTempPath()) ('alpineos-export-' + [guid]::NewGuid().ToString('N'))
$staging = Join-Path $temp 'SkiResort-Management'
$foldersToSkip = @('.git', '.vs', '.vscode', '.idea', 'node_modules', 'dist', 'bin', 'obj', '.vite', '.cache', 'coverage', 'backups', '__pycache__', 'test-results', 'playwright-report')
$extensionsToSkip = @('.db', '.sqlite', '.sqlite3', '.dump', '.bak', '.pfx', '.p12', '.pem', '.key', '.pyc')
$filesToSkip = @('.env', 'secrets.json', 'appsettings.Development.local.json')
New-Item -ItemType Directory -Force -Path $staging | Out-Null
try {
  $count = 0
  Get-ChildItem -LiteralPath $ProjectRoot -Recurse -File -Force | ForEach-Object {
    $source = $_
    $relative = $source.FullName.Substring($ProjectRoot.Length).TrimStart('\','/')
    $pieces = $relative -split '[\\/]'
    if (@($pieces | Where-Object { $foldersToSkip -contains $_ }).Count -gt 0) { return }
    if ($filesToSkip -contains $source.Name) { return }
    if ($source.Name -match '^\.env\.' -and $source.Name -ne '.env.example') { return }
    if ($extensionsToSkip -contains $source.Extension.ToLowerInvariant()) { return }
    if ($source.Name -like '*.sql*' -or $source.Name -like '*backup*.zip') { return }
    if ($source.Length -gt 40MB) { Write-Warning "Atlandi: cok buyuk dosya: $relative"; return }
    $dest = Join-Path $staging $relative
    New-Item -ItemType Directory -Force -Path (Split-Path $dest -Parent) | Out-Null
    Copy-Item -LiteralPath $source.FullName -Destination $dest -Force
    $count += 1
  }
  if ($count -lt 15) { throw 'Cok az dosya bulundu; proje ana klasorunden calistirdiginizdan emin olun.' }
  if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Force }
  Compress-Archive -Path $staging -DestinationPath $target -CompressionLevel Optimal
  $screens = @(Get-ChildItem -LiteralPath (Join-Path $staging 'docs/screenshots') -File -Filter '*.png' -ErrorAction SilentlyContinue)
  Write-Host "Hazirlanan kaynak dosyasi sayisi: $count" -ForegroundColor Green
  Write-Host "Ekran goruntuleri: $($screens.Count) / 16" -ForegroundColor Cyan
  Write-Host "Arsiv: $target" -ForegroundColor Green
  Write-Warning 'Yuklemeden once arsivde gercek kisi verisi, ozel anahtar veya baska gizli bilgi olmadigindan emin olun.'
}
finally {
  Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
}
