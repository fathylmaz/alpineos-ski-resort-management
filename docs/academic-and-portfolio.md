# Akademik bağlam ve portföy dokümanı

## Projenin adı

**Kayak Merkezi Otomasyonu — AlpineOS Modernizasyonu**

## Problem ve hedef

Kayak merkezlerinde müşteriler, ekipmanlar, konaklama birimleri, pistler, etkinlikler, personel ve ücret işlemleri birbirinden kopuk takip edildiğinde veri tekrarları, çakışan rezervasyonlar ve operasyonel gecikmeler oluşabilir. Birlikte yönetilmesi gereken bu iş süreçlerinin tek bir yönetim panelinde birleştirilmesi hedeflenmiştir.

## Üniversite dönemindeki ilk çalışma

Orijinal dosyalarda Microsoft Access veritabanı, proje raporu ve ilişki diyagramları mevcuttur. Raporda kiralama, personel, envanter ve müşteri işlemlerinin merkezi otomasyonu hedeflenir. Kaynak pakette Windows Forms uygulamasının `.sln` / `.cs` kodları bulunmadığı için eski uygulamanın tüm davranışları doğrulanmamıştır.

## Modernizasyonun kapsamı

- Access odaklı çalışma yerine ASP.NET Core 10 + PostgreSQL 17 sunucu katmanı.
- React/TypeScript ile ayrı istemci katmanı ve responsive UI.
- Parola hash'leme, cookie auth, CSRF, roller ve işlem günlükleri.
- Aynı ekipman/oda için çakışan rezervasyonları PostgreSQL seviyesinde engelleme.
- Kiralama durumu, iade ve iptal süreçleri.
- Etkinlik seansları, kontenjan ve pist durumu yönetimi.
- Ödeme entegrasyonu olmayan manuel finans kayıtları, yönetim raporlaması.
- Docker yapılandırması, birim testi, smoke test ve CI iskeleti.

## Ölçülebilir kalite hedefleri

- 8 saf domain birim testi tanımlanmıştır; derlenip çalıştırılması gerekir.
- Rezervasyonlar için veritabanı düzeyi çakışma kısıtları bulunur.
- API oturumsuz istekleri 401; yetkisiz yazmaları 403 ile reddeder.
- Tek `docker compose up --build` ile yerel ortamın kurulabilmesi hedeflenir.
- Testler tamamen doğrulanmadan 'tamamı sorunsuz çalışıyor' denilmemelidir.

## LinkedIn için doğru ifade

"Üniversite döneminde geliştirdiğim Kayak Merkezi Otomasyonu veritabanı projesini, orijinal iş problemini ve süreçlerini koruyarak güncel teknoloji yığınıyla yeniden ele aldım. Modernizasyon sürümünde ekipman kiralama, konaklama rezervasyonu, pist/ders yönetimi, rol tabanlı personel işlemleri ve PostgreSQL veri tutarlılığı kontrolleri tasarladım."

Bu ifade modernizasyonun üniversite sonrası geliştirildiğini açıkça ayırır; tamamlandığı kanıtlanamayan testleri başarı olarak sunmaz.

## CV için önerilen teknik madde

**AlpineOS — Kayak Merkezi Yönetim Sistemi (üniversite projesinin modernizasyonu)** | C#, ASP.NET Core, PostgreSQL, EF Core, React, TypeScript, Docker

Müşteri, ekipman, kiralama, konaklama, pist ve etkinlik süreçlerini tek panelde birleştiren rol bazlı web uygulaması; PostgreSQL constraint tabanlı rezervasyon çakışma önleme, audit logging ve manuel finans hareketleri.
