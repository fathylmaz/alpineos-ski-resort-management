# AlpineOS v2 teslimat ve doğrulama notu

## Eklenen ve değiştirilen kaynaklar

- `api/Features/CustomerEndpoints.cs`: kayıt, giriş, profil, misafir kataloğu, müsaitlik, kişiye özel kiralama / oda / ders / ödeme geçmişi, kendi kayıtlarını oluşturma ve güvenli iptal.
- `api/Models/Entities.cs`, `api/Data/ResortDb.cs`, `api/Program.cs`: müşteri parola hash'i, kilitlenme, kimlik rolü, staff/customer politika ayrımı ve mevcut PostgreSQL verisi için additive şema yükseltmesi.
- `api/Data/DbSeeder.cs`: **opsiyonel** zengin örnek katalog, personel, departman, pist, etkinlik ve demo işlem kayıtları.
- `api/Features/{Master,Booking,Activity,Finance,Dashboard}Endpoints.cs`: staff API gruplarına personel politikası ekleme; müşteri yönetici paneli listelerini okuyamaz.
- `web/src/GuestAccess.tsx`: müşteri kayıt/giriş ekranı.
- `web/src/CustomerPortal.tsx` ve `web/src/customer.css`: responsive müşteri paneli, 8 bölüm, rezervasyon tarih alanları ve müsaitlik kontrolü.
- `web/src/App.tsx` ve `web/src/styles.css`: orijinal yönetici arayüzünü koruyan giriş / kullanıcı deneyimi geçişi.
- `scripts/customer_integration_test.py`: API'nin gerçek PostgreSQL üzerinde entegrasyonuna ait test senaryoları (**çalıştırılmadı**).

## Bu teslimatta yapılan kontroller

- TS ve TSX dosyaları TypeScript parser ile sözdizimi açısından kontrol edildi.
- JSON, csproj XML, Python betikleri parse edildi.
- Özellikle müşterinin admin GET endpointlerine erişiminin politika düzeyinde engellenmesini, müşteri kaynaklarının kimlikten filtrelenmesini ve benzersiz portal e-posta index'ini statik olarak denetledik.
- Proje ZIP'in bütünlüğü ve SHA-256 manifesti ayrı doğrulandı.

## Bu ortamda geçilmiş olarak beyan edilmeyen testler

- `.NET 10` build ve xUnit: dotnet SDK bulunmadı.
- Docker Compose üzerinde PostgreSQL ile gerçek uygulama başlatma: Docker yok.
- npm bağımlılık kurulumu ve React typecheck / Vite üretim derlemesi: npm install zaman aşımına uğradı.
- İki panelli tam tarayıcı E2E: çalıştırılmadı.

Bu nedenle bu sürüm **kaynak kodu tamamlanmış bir geliştirme teslimatı**dır; tüm çalışma senaryoları başarılı doğrulanmış bir üretim sürümü olduğu iddia edilmez. Kullanıcı kendi bilgisayarında kontrol etmeli, varsa build/runtime hatalarının giderilmesi sonrası GitHub ve LinkedIn için üretim sürümü diye sunmalıdır.

## Yerel doğrulama komutları

```powershell
# Kurulum klasörü içinden
# .env dosyanız zaten varsa üzerine yazmayın.
docker compose up -d --build
docker compose ps
docker compose logs --tail=100 api

dotnet test tests/SkiResort.Tests/SkiResort.Tests.csproj
cd web
npm install
npm run build
cd ..
```

Entegrasyon testi **yalnızca atılabilir demo veritabanında**:

```powershell
$env:ADMIN_EMAIL=".env DOSYANIZDAKİ_YÖNETİCİ_EMAIL"
$env:ADMIN_PASSWORD=".env DOSYANIZDAKİ_YÖNETİCİ_PAROLASI"
$env:I_CONFIRM_DISPOSABLE_DB="YES"
python scripts/customer_integration_test.py
```

Daha fazla bilgi: [customer-portal.md](customer-portal.md)
