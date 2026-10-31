# Katkı ve geliştirme notları

AlpineOS, üniversite projesi temelinden geliştirilen bir portföy uygulamasıdır.

## Yerel geliştirme

1. `.env.example` dosyasını `.env` olarak kopyalayın ve gizli ayarları değiştirin.
2. `docker compose up -d --build` ile sistemi başlatın.
3. API iş kurallarını değiştirdiyseniz `dotnet test tests/SkiResort.Tests/SkiResort.Tests.csproj` çalıştırın.
4. Arayüz değiştirdiyseniz `cd web` altında `npm install` ve `npm run build` çalıştırın.
5. Değişiklikleri küçük, anlaşılır commit'lerle gönderin.

## Güvenlik ve veri

Gerçek `.env` dosyaları, veritabanı dökümleri, kişisel veriler, şifreler ve erişim anahtarları depoya yüklenmemelidir.
Ekran görüntülerinde yalnızca kurgusal demo verileri kullanılmalıdır.
Üretim ortamına hazırlık sınırlamaları için `SECURITY.md` dosyasını inceleyin.
