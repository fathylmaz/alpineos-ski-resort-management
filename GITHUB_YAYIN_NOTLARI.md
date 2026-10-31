# AlpineOS GitHub yayın notları

Bu paket, yerel olarak test edilmiş proje kaynak kodunu, 16 gerçek ekran görüntüsünü, raporları ve teknik belgeleri içerir.

## Tek işlemle ilk yayın

1. GitHub hesabındaki `fathylmaz/alpineos-ski-resort-management` deposunun boş olduğundan emin ol.
2. Proje klasöründe `YAYINLA_GITHUB.cmd` dosyasını çalıştır.
3. GitHub giriş ekranı çıkarsa yalnızca GitHub uygulaması / Git Credential Manager üzerinden hesabına giriş yap. **Parolanı veya tokenini ChatGPT'ye gönderme.**
4. `YAYIN TAMAMLANDI` mesajı görününce GitHub depo adresini aç.

`YAYINLA_GITHUB.cmd`, güvenilir `publish_to_github.ps1` betiğini **yalnızca bu çalıştırmada** PowerShell güvenlik politikasını geçerek çalıştırır; global ayarları değiştirmez. Dosya içeriklerini okuyarak çalıştırman önerilir.

Betiğin korumaları:
- Mevcut çalışan projeni değiştirmez. GitHub'a gönderilecek dosyaları geçici bir klasöre kopyalar.
- `.env`, gizli dosyalar, sertifikalar, veritabanları, loglar ve derleme çıktıları kopyalanmaz.
- 16 ekran görüntüsü ve README bağlantılarını yayından önce kontrol eder.
- Hedef depo boş değilse otomatik üzerine yazmaz ve hata verir.
- GitHub kimlik doğrulaması Git'in kurulu giriş sistemi ile yapılır.

## Yayın sonrasında

- [ ] GitHub Actions, backend ve frontend işlerinin sonuçlarını kontrol et
- [ ] README'de 16 ekran görüntüsünü ve PDF bağlantılarını açarak doğrula
- [ ] GitHub > About bölümünde açıklama ve konu etiketlerini düzenle
- [ ] Gerçek müşteri / kimlik bilgisi içeren görseller olmadığını tekrar doğrula

**Önerilen açıklama:** Full-stack ski resort management system with admin and customer portals, equipment rentals, accommodation booking, and PostgreSQL.

**Önerilen etiketler:** `aspnetcore`, `csharp`, `react`, `typescript`, `postgresql`, `docker`, `booking-system`, `ski-resort`, `full-stack`, `university-project`.

**Not:** GitHub Actions doğrulaması yapılmadan otomatik testlerin tamamı geçmiş sayılmamalıdır.
