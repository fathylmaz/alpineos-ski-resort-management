# İşletim rehberi

## Durum ve loglar

```powershell
docker compose ps
docker compose logs -f --tail=100 api
docker compose logs -f --tail=100 web
docker compose logs -f --tail=100 db
```

Frontend (Nginx) sadece `127.0.0.1:8088` dinler. PostgreSQL ve API host portuna açılmaz. `GET /api/health` uygulama sağlığı, `GET /healthz` middleware sağlığıdır; ilk endpoint veritabanının çalıştığını garanti etmez.

## Güvenli yerel yedek

```powershell
.\scripts\backup.ps1
```

`backups/*.sql` dosyalarını açık GitHub reposuna koymayın; gerçek müşteri kayıtları içerebilir. Yedekleme ve geri dönüşün işlenme yöntemi altyapıya göre uyarlanmalı, yedekler şifrelenmeli ve saklama süresi tanımlanmalıdır.

## Parola değişiklikleri

`ADMIN_PASSWORD` yalnız **ilk admin oluşturulduğunda** kullanılır. Sonradan `.env`'de değeri değiştirmek mevcut hesabın parolasını güncellemez. Parola sıfırlama self servis akışı bu sürümde yoktur. Gerçek kullanımda güvenli yönetici sıfırlama prosedürü uygulanmalıdır.

## Veri silme / geliştirme sıfırlaması

```powershell
# Yalnızca kurgusal demo ortamında, geri dönüşü olmayan şekilde veriyi silmek için:
docker compose down -v
```

**Gerçek veride kullanmayın.**

## İnternete yayınlamadan önce

- TLS terminasyonu (domain + sertifika + otomatik yenileme).
- `ASPNETCORE_ENVIRONMENT=Production`, `SEED_DEMO_DATA=false`; gerçek sırlar Docker Secrets / secret manager'dan.
- Nginx / API forwarded headers ve güvenilir proxy ayarları, rate limits, CORS, güvenlik başlıkları, oturum/CSRF senaryolarını canlı domain üzerinde doğrulayın.
- Artık geliştirme odaklı `EnsureCreated` değil migration akışı.
- Fail2ban/WAF, merkezi log, alarm, izleme ve güvenlik güncelleme politikaları.
- Gerçek müşteri ve finans verisi için uygun hukuki süreç ve yetkilendirme.

Bu paket tek komutla **yerel geliştirme ortamı** içindir; doğrudan internete açılmış kurumsal production deployment olarak tasarlanmamıştır.
