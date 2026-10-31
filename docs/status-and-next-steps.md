## v2 kapsamı
Müşteri portalı ve artan demo kataloğu kaynak kodu eklendi; bu hazırlama ortamında .NET/Docker derlemesi ve uçtan uca servis testi çalıştırılamadı. `docs/customer-portal.md` geçiş ve kontrol senaryolarını içerir.

# Uygulama Durumu ve Son Doğrulama Planı

## Hazır kodlar

- [x] C# ASP.NET Core API ve Entity Framework PostgreSQL modelleri
- [x] Çakışma kısıtlarıyla kiralama ve konaklama
- [x] Müşteri, personel, envanter, bakım, pist, ders, finans işlemleri
- [x] Cookie kimlik doğrulama ve role-protected yazma API'leri
- [x] React ve TypeScript responsive yönetim paneli
- [x] Docker, Nginx, .env şablonu
- [x] Örnek kurgusal başlangıç verisi
- [x] xUnit kurallar testi, smoke ve entegrasyon senaryoları kaynak kodu
- [x] GitHub Actions, API/mimari/operasyon/portföy belgeleri

## Statik doğrulama — tamamlandı

- [x] React/TypeScript TS ayrıştırma (syntax diagnostics 0)
- [x] Python test betikleri sözdizimi
- [x] JSON, YAML, XML geçerliliği
- [x] Dizin/dosya yapısı kontrolü

## Kullanıcı ortamında yapılacak doğrulama — henüz çalıştırılmadı

- [ ] `.NET 10` restore/build/unit test
- [ ] `npm install`, `npm run build` (typecheck+Vite)
- [ ] `docker compose up -d --build` ve API/SPA kontrolü
- [ ] `scripts/smoke_test.py`
- [ ] İzole veritabanında `scripts/integration_scenarios.py`
- [ ] Mobil tarayıcı/erişilebilirlik testi
- [ ] Üretim migration, TLS, güvenlik, yedek geri dönüş provası

**Gerekçe:** Dosyaların üretildiği ortamda .NET CLI/Docker Engine bulunmamaktadır; npm paket kayıt sunucusuna erişim de mümkün olmamıştır. Dolayısıyla bu doğrulamalar için sahte başarı raporu verilmemektedir.

## İlk çalışma komutları

```powershell
Copy-Item .env.example .env
# Güçlü parolaları gir
docker compose up -d --build
docker compose ps
docker compose logs --tail=150 api
```

Gerçek bir çalıştırma hatası çıkarsa API derleyici/çalıştırıcı logları üzerinden düzeltme yapılıp yeni sürüm alınmalıdır. Projenin GitHub, LinkedIn ve CV aşamasına geçirilmesi için yalnızca dosyaların varlığı yeterli kabul edilmez; çalışma ve test çıktıları doğrulanmalıdır.
