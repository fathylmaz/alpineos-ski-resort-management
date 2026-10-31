**v2:** The customer identity/booking/permission integration suite is available in `scripts/customer_integration_test.py`. It must run against a disposable DB with `I_CONFIRM_DISPOSABLE_DB=YES`. See [customer-portal.md](customer-portal.md).

# Test yaklaşımı, kontrol listesi ve kabul kriterleri

## Otomatik testler — pakette bulunanlar

1. **xUnit / RulesTests**: bitişik rezervasyonlar, çakışma, kısmi gün ücret, tam gün ücret, gece fiyatlama, geçersiz tarih ve UTC kontrolü.
2. **scripts/smoke_test.py**: sağlık endpointi, anonim erişimin reddi, CSRF zorunluluğu, admin oturumu, tüm temel liste endpointleri.
3. **GitHub Actions**: .NET derleme / xUnit, TypeScript/Vite derleme. Smoke test için API ve Postgres çalışması gerekir.

## İlk doğrulama (PowerShell)

```powershell
Copy-Item .env.example .env
# .env içindeki iki parolayı değiştirin
docker compose up --build -d
docker compose ps
docker compose logs --tail=80 api

dotnet test tests/SkiResort.Tests/SkiResort.Tests.csproj
cd web
npm install
npm run build
cd ..

$env:ADMIN_EMAIL='admin@example.com'
$env:ADMIN_PASSWORD='GERCEK_PAROLANIZ'
python scripts/smoke_test.py
```

## Kabul senaryoları (elle / entegrasyonla doğrulanacak)

| ID | İşlem | Beklenen |
|---|---|---|
| AUTH-01 | Oturumsuz `GET /api/dashboard` | HTTP 401 |
| AUTH-02 | CSRF olmadan `POST /api/customers` | HTTP 400 |
| AUTH-03 | Cashier ile ekipman `POST` | HTTP 403 |
| AUTH-04 | Yanlış parolayla 5 ardışık giriş | Geçici kilit |
| EQ-01 | Aynı `AssetCode` ile 2 ekipman açılması | HTTP 409 |
| EQ-02 | Bakımdaki ekipmanla rezervasyon | HTTP 409 |
| RENT-01 | Aynı varlık/aynı aralıkta 2 aktif rezervasyon | Birinci 201, ikinci 409 |
| RENT-02 | Birinci rezervasyonun bitiş saatinde ikinci başlangıç | İkisine de izin |
| RENT-03 | Reserve→Checkout→Return geçişi | Sadece bu sırayla |
| RENT-04 | İptal edilmiş kiralamaya checkout | HTTP 409 |
| RENT-05 | İstemciden ücret gönderme | API fiyatı kendi hesaplar |
| STAY-01 | Aynı oda için çakışma | HTTP 409 |
| STAY-02 | Kapasite üstünde misafir | HTTP 400 |
| STAY-03 | Yan yana gece aralıkları | İzin |
| ACT-01 | Kapasitesi 1 olan derse iki ayrı müşteri kayıt | En çok 1 kayıt |
| ACT-02 | Aynı müşteri aynı derse tekrar kayıt | HTTP 409 |
| PAY-01 | 1000 TL rezervasyona 500 + 500 TL kayıt | Başarılı |
| PAY-02 | Tam ödenmiş kayda 1 TL ek tahsilat | HTTP 409 |
| PAY-03 | Net ödenenden fazla iade | HTTP 409 |
| PAY-04 | Aynı `ExternalReference` ile çift kayıt | HTTP 409 |
| AUD-01 | Rezervasyon, iptal, bakım, ödeme sonrası audit | Olay oluşur |
| UI-01 | Masaüstü ve telefon arayüzleri | Bölümler erişilebilir ve form gönderilir |
| OPS-01 | `docker compose down` ve `up` | Veri volume içinde kalır |

**Not:** Tablo kabul testlerinden yalnızca xUnit ve smoke testte belirtilen alt küme otomatikleştirilmiştir. Arşivde veritabanlı ve tarayıcılı tüm senaryoların çalıştırılıp geçtiğine ilişkin kanıt bulunmaz.

## Yarış koşulları

API'nin PostgreSQL `EXCLUDE` kısıtlarını uyguladığı kabul, **gerçek PostgreSQL üzerinde eş zamanlı** iki transaction ile ayrıca test edilmelidir. Uygulama seviyesindeki `AnyAsync` kontrolünün iki süreçte de false dönmesi mümkündür; esas garantiyi veritabanı constraint'i verir. Ders kontenjanı/ödemeler `Serializable` transaction kullanır; eşzamanlı çakışmada 409'a düşmesi beklenir.

## Gerçek ortama geçmeden önce

- EF migration ve geri dönüş testleri.
- Gerçek TLS, role-permission matrisi ve KVKK denetimi.
- Otomatik tarayıcı E2E, erişilebilirlik, yük ve stres testi.
- Güvenlik taraması, uygulama ve DB yedeğinden geri dönüş provası.

## Genişletilmiş senaryo betiği

Uygulama çalışıyorsa **yalnızca yeni/atılabilir demo veritabanı üzerinde**:

```powershell
$env:ADMIN_EMAIL='admin@example.com'
$env:ADMIN_PASSWORD='GERCEK_PAROLANIZ'
$env:I_CONFIRM_DISPOSABLE_DB='YES'
python scripts/integration_scenarios.py
```

Bu betik kurgusal müşteriler, ekipman, oda, etkinlik, kiralama ve ödeme kayıtları yaratır ve **silmez**. Benzersiz kod, rezervasyon çakışması, fiyat, kapasite ve ödemede çift kayıt gibi senaryoları HTTP üzerinden kontrol eder.
