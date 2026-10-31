# Run from the repository root: .\scripts\check_screenshots.ps1
$names = @(
    "01-admin-login.png",
    "02-admin-dashboard.png",
    "03-equipment-inventory.png",
    "04-rental-management.png",
    "05-accommodation.png",
    "06-slopes.png",
    "07-lessons-events.png",
    "08-finance.png",
    "09-departments-staff.png",
    "10-customer-login.png",
    "11-customer-home.png",
    "12-customer-equipment.png",
    "13-customer-booking.png",
    "14-customer-stays.png",
    "15-my-bookings.png",
    "16-mobile-customer.png"
)
$missing = @($names | Where-Object { -not (Test-Path -LiteralPath (Join-Path 'docs/screenshots' $_)) })
if ($missing.Count -gt 0) {
    Write-Host "Eksik ekran goruntusu: $($missing.Count)" -ForegroundColor Yellow
    $missing | ForEach-Object { Write-Host "  - $_" }
    exit 1
}
Write-Host "16 ekran goruntusunun tamami hazir." -ForegroundColor Green
