"""Review the expected README assets before publishing the project."""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
README = (ROOT / "README.md").read_text(encoding="utf-8")
EXPECTED = [
    "01-admin-login.png", "02-admin-dashboard.png", "03-equipment-inventory.png",
    "04-rental-management.png", "05-accommodation.png", "06-slopes.png",
    "07-lessons-events.png", "08-finance.png", "09-departments-staff.png",
    "10-customer-login.png", "11-customer-home.png", "12-customer-equipment.png",
    "13-customer-booking.png", "14-customer-stays.png", "15-my-bookings.png",
    "16-mobile-customer.png",
]
missing = [name for name in EXPECTED if not (ROOT / "docs/screenshots" / name).is_file()]
not_linked = [name for name in EXPECTED if "docs/screenshots/" + name not in README]
for entry in ["docs/diagrams/mimari.png", "docs/diagrams/veri_modeli.png",
              "docs/diagrams/rezervasyon_akisi.png",
              *[f"docs/reports/{i:02d}_{n}.pdf" for i, n in enumerate(
               ["Proje_Raporu", "Teknik_Mimari_Veri_Modeli", "Kullanim_Kilavuzu",
                "Test_Kalite_Notlari", "Ekran_Goruntusu_Rehberi", "Proje_Sunumu"], start=1)]]:
    if not (ROOT / entry).is_file(): print("Eksik teknik belge:", entry)
print("README'de bağlı ekran görseli:", len(EXPECTED) - len(not_linked), "/ 16")
print("Yerel olarak bulunan ekran görseli:", len(EXPECTED) - len(missing), "/ 16")
if missing: print("Henüz yüklenmeyen gerçek ekran görüntüleri:" + "".join("\n - " + x for x in missing))
if not_linked: print("README bağlantısı eksik:", ", ".join(not_linked))
if (ROOT / ".env").exists(): print("UYARI: .env mevcut; GitHub'a eklemeyin!")
sys.exit(1 if missing or not_linked else 0)
