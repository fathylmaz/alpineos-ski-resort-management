$ErrorActionPreference = 'Stop'
if (!(Test-Path '.env')) {
    Copy-Item '.env.example' '.env'
    Write-Host 'Created .env. Edit ADMIN_PASSWORD and POSTGRES_PASSWORD before startup.' -ForegroundColor Yellow
    exit 0
}
Write-Host 'Starting AlpineOS containers...' -ForegroundColor Cyan
docker compose up -d --build
Write-Host 'Open http://localhost:8088' -ForegroundColor Green
