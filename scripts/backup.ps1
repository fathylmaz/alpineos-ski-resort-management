$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force backups | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backup = "backups/skiresort-$stamp.sql"
docker compose exec -T db sh -c 'pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB"' | Out-File -Encoding utf8 $backup
Write-Host "Backup created: $backup"
