<#
AlpineOS safe first publication to an EMPTY GitHub repository.
This script uses Git on the user's own machine because the repository needs
binary screenshots/PDFs that cannot be pushed through the connected chat tool.
It does NOT change the user's working project or delete any GitHub files.
#>
param(
  [string]$RepositoryUrl = 'https://github.com/fathylmaz/alpineos-ski-resort-management.git',
  [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-Git {
  param([string[]]$Arguments)
  & git @Arguments
  if ($LASTEXITCODE -ne 0) { throw "Git command failed: git $($Arguments -join ' ')" }
}

if (-not (Get-Command git -ErrorAction SilentlyContinue)) { throw 'Git yuklu degil. Git for Windows kurulu olmali.' }
$root = (Resolve-Path -LiteralPath $ProjectRoot).Path.TrimEnd([char[]]@('\', '/'))
foreach ($relative in @('README.md','compose.yaml','api/SkiResort.Api.csproj','web/package.json','.gitignore')) {
  if (-not (Test-Path -LiteralPath (Join-Path $root $relative))) { throw "Proje dosyasi eksik: $relative" }
}
$names = @(
'01-admin-login.png','02-admin-dashboard.png','03-equipment-inventory.png',
'04-rental-management.png','05-accommodation.png','06-slopes.png',
'07-lessons-events.png','08-finance.png','09-departments-staff.png',
'10-customer-login.png','11-customer-home.png','12-customer-equipment.png',
'13-customer-booking.png','14-customer-stays.png','15-my-bookings.png',
'16-mobile-customer.png')
$readme = Get-Content -LiteralPath (Join-Path $root 'README.md') -Raw -Encoding UTF8
foreach ($name in $names) {
  if (-not (Test-Path -LiteralPath (Join-Path $root "docs/screenshots/$name"))) { throw "Ekran goruntusu eksik: $name" }
  if (-not $readme.Contains("docs/screenshots/$name")) { throw "README baglantisi eksik: $name" }
}
$skipFolders = @('.git','.vs','.vscode','.idea','node_modules','dist','bin','obj','.vite','.cache','coverage','backups','__pycache__','test-results','playwright-report','keys')
$skipNames = @('.env','secrets.json','appsettings.Development.local.json','Thumbs.db','.DS_Store')
$skipExtensions = @('.db','.sqlite','.sqlite3','.dump','.bak','.pfx','.p12','.pem','.key','.pyc','.zip')
$stage = Join-Path ([IO.Path]::GetTempPath()) ('alpineos-publish-' + [guid]::NewGuid().ToString('N'))
Write-Host 'GitHub icin hazirlik kontrolu: 16/16 ekran goruntusu mevcut.' -ForegroundColor Green
Write-Host 'GitHub deposu:' $RepositoryUrl
Write-Host 'Calisan proje degistirilmeyecek; yayin icin ayri gecici klasor olusturulacak.'

try {
  Invoke-Git -Arguments @('clone', $RepositoryUrl, $stage)
  # Never replace unrelated or previously published work without review.
  & git -C $stage rev-parse --verify --quiet HEAD | Out-Null
  if ($LASTEXITCODE -eq 0) { throw 'Depo artik bos degil. Uzerine yazmadan once degisiklikleri incelememiz gerekiyor.' }
  Invoke-Git -Arguments @('-C',$stage,'checkout','-B','main')
  $copied = 0
  Get-ChildItem -LiteralPath $root -Recurse -File -Force | ForEach-Object {
    $file = $_
    $relative = $file.FullName.Substring($root.Length).TrimStart([char[]]@('\', '/'))
    $segments = $relative -split '[\\/]'
    if (@($segments | Where-Object { $skipFolders -contains $_ }).Count -gt 0) { return }
    if ($skipNames -contains $file.Name) { return }
    if ($file.Name -match '^\.env\.' -and $file.Name -ne '.env.example') { return }
    if ($skipExtensions -contains $file.Extension.ToLowerInvariant()) { return }
    if ($file.Name -match '(^|[._-])backup([._-]|$)' -and $file.Name -notmatch '\.ps1$') { return }
    if ($file.Length -gt 40MB) { throw "40 MB uzeri beklenmeyen dosya: $relative" }
    $dest = Join-Path $stage $relative
    New-Item -ItemType Directory -Path (Split-Path -Parent $dest) -Force | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $dest
    $copied++
  }
  if ($copied -lt 80) { throw "Beklenen proje dosyalarinin bir kismi eksik olabilir ($copied dosya)." }
  Invoke-Git -Arguments @('-C',$stage,'add','--all')
  Invoke-Git -Arguments @('-C',$stage,'diff','--cached','--check')
  $staged = @(& git -C $stage diff --cached --name-only)
  foreach ($name in $staged) {
    if ($name -match '(^|/)(\.env($|\.)|secrets\.json$|\.git/)' -and $name -notmatch '(^|/)\.env\.example$') { throw "Gizli olabilecek dosya stage edildi: $name" }
  }
  Write-Host "Yayina alinacak dosya sayisi: $($staged.Count)" -ForegroundColor Cyan
  Invoke-Git -Arguments @('-C',$stage,'commit','-m','feat: publish AlpineOS ski resort management system')
  Write-Host 'GitHub oturum acma ekrani acilirsa kendi hesabinla giris yap.' -ForegroundColor Yellow
  Invoke-Git -Arguments @('-C',$stage,'push','-u','origin','main')
  Write-Host 'YAYIN TAMAMLANDI:' -ForegroundColor Green
  Write-Host 'https://github.com/fathylmaz/alpineos-ski-resort-management' -ForegroundColor Green
  Write-Host "Yayin kopyasi: $stage" -ForegroundColor DarkGray
}
catch {
  Write-Host "Yayin tamamlanamadi: $($_.Exception.Message)" -ForegroundColor Red
  Write-Host "Kontrol edilebilecek gecici klasor: $stage" -ForegroundColor Yellow
  throw
}
