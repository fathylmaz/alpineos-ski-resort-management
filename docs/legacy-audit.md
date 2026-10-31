# 01 — Orijinal Proje Denetimi

## İncelenen dosyalar

İncelenen arşiv: `Kayak Merkezi Otomasyonu.zip` (8 ZIP girdisi, bunların 2'si klasör).

| Dosya | Ne içeriyor | Bulgular |
|---|---|---|
| `Kayak Merkezi.accdb` | Microsoft Access veritabanı | Veritabanı dosyası gerçek; ikili düzeyde tablo adları görülebiliyor. Tam satır ve alan türü okuması bu incelemede yapılmadı. |
| `Kayak Merkezi.laccdb` | Access kilit/oturum dosyası | Uygulama kaynak dosyası değil; taşınmaz. |
| `Ekran görüntüsü 2024-05-17 014016.png` | Koyu renkli ilişkisel şema | `.accdb` içinden tespit edilen tablo adlarıyla örtüşüyor. |
| `a.jpg` | Ayrı Access ilişkisel şeması | Ders, yarışma, ziyaretçi ve yetkilendirme ağırlıklı; aynı şemanın kanıtı sayılmamalı. |
| `Rapor.docx`, `Rapor.pdf` | Proje raporu, ikisi de esasen aynı anlatımı taşıyor | Amaç, araçlar ve 7 geliştirme adımı. Uygulama ekranları, kod ve SQL komutları yok. |

## Projenin özgün odağı

Raporda kiralama, müşteri, personel, envanter ve satış/fatura akışlarının tek sistemde toplanması hedefleniyor. Raporda SQL Server Management Studio, Visual Studio 2022 / Windows Forms, Microsoft Access, function/stored procedure/trigger kullanımı anlatılıyor. Ancak ZIP içinde C# uygulaması, SQL Server yedek veya şema betikleri, trigger veya stored procedure kodları **bulunmadığından bu parçaların gerçekten uygulanıp uygulanmadığı doğrulanamıyor**.

## `.accdb` ile örtüşen koyu ilişki diyagramındaki 10 tablo

1. `Etkinlikler` — Etkinlik adı, tarih/saat, eğitmen/rehber, açıklama, ücret.
2. `İptalEdilenKiralar` — Kiralama ID, iptal nedeni ve tarihi.
3. `KiralıkEkipmanlar` — Ekipman adı/türü, kiralama ücreti, stok durumu.
4. `KiralamaGeçmişi` — Kullanıcı, ekipman, ev, kiralama/iade tarihi ve ücret.
5. `Kullanıcılar` — Ad, soyad, e-posta, telefon, adres.
6. `Ödemeler` — Kullanıcı, tutar, tarih, tür.
7. `Konaklama` — Ev, konum, oda, kapasite, olanaklar, kira ücreti.
8. `PistDurumu` — Pist, kar kalınlığı, hava durumu, kullanılabilirlik, son güncelleme.
9. `Departmanlar` — Ad, müdür, telefon, konum.
10. `Personel` — Ad, soyad, pozisyon, departman, iletişim.

Not: Alanların ilişkileri şemada görüntülenmiştir, bu incelemede Access dosyasının tüm kolon türleri ve kısıtları veritabanı sürücüsü üzerinden doğrulanmamıştır.

## `a.jpg` içindeki farklı tasarım

Bu görselde `DersKayıtları`, `Ziyaretçiler`, `EkipmanKiralama`, `Ekipmanlar`, `Dersler`, `DersZamanıTablosu`, gösteri/yarışma programı ve zamanı, `Kullanıcılar`, `Yetkilendirme` yer alıyor. `.accdb` ile eşleşen şemada bu adlar doğrulanmadı. Görsel, olası farklı sürüm veya ek tasarım fikri olarak saklanır.

## Tespit edilen modelleme iyileştirme fırsatları

- `KiralamaGeçmişi`nde ekipman ve evin birlikte bulunması farklı rezervasyon türlerini karıştırıyor: kira sözleşmesi/satırları ile konaklama rezervasyonu ayrılmalı.
- Ekipman için serbest bir `stok durumu` alanı, tekil fiziksel ekipman ve gerçek zamanlı müsaitlik takibine yetmez.
- Ödeme sadece kullanıcıya bağlanmamalı; sipariş/borç, kısmi/iade ödemeleri ve mutabakatla ilişkilendirilmeli.
- Tarih aralığına göre oda/ekipman çakışmaları ve kapasite kontrolü veritabanı seviyesinde korunmalı.
- Silme/iptal yerine duruma dayalı işlem geçmişi, izlenebilirlik ve denetim kaydı bulunmalı.
- Personel departmanı ve müdür ilişkileri normalize edilmeli.
- Ayrı görseldeki `şifre` sütunu nedeniyle parola saklama yöntemi doğrulanmamıştır; yeni sistemde **asla düz metin parola saklanmayacaktır**.

## Bilinen sınırlar

Kaynak pakette UI uygulaması, veri tabanı komut dosyaları, çalışma ekranları ve otomatik testler bulunmadığı için uygulama davranışları test edilemedi. Eski tablo içeriklerinin tam envanteri ve örnek veri aktarım planı, uygun Access okuyucu ile sonraki aşamada kesinleştirilmelidir.
