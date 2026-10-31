# AlpineOS v2 — Misafir portalı ve geniş demo kataloğu

## İstenen geliştirmeler

Eski yönetim paneli **korunur**. Eklenen panel, bağımsız prototip veya yerel tarayıcı verisi değildir: her iki panel de aynı ASP.NET Core API ve PostgreSQL tablolarını kullanır.

### Yeni modüller

- Personel girişindeki **Misafir / Müşteri Paneline Geç** düğmesi ile misafir erişimi
- Müşteri e-posta + en az 12 karakter şifreyle kaydolma, giriş ve güvenli çıkış
- Kayak envanteri, kategori/beden/fiyat bilgisi; ekipman seçip başlangıç/bitiş tarihiyle rezervasyon
- Oda/dağ evi kataloğu; kişi sayısı, kapasite, giriş/çıkış tarihleriyle konaklama rezervasyonu
- Ders, yarışma ve etkinlik listeleme; kontenjan kontrolüyle kayıt ve iptal
- Açık/kapalı pist ve kar kalınlığı bilgileri
- Sadece hesaba ait rezervasyon, konaklama, ders kaydı ve ödeme hareketleri
- Müşteri tarafından, henüz başlamamış ve ödeme hareketi olmayan rezervasyonların iptali
- Profil / telefon güncelleme (e-posta değişikliği desteklenmiyor)
- Sunucudan tarih aralığına göre müsaitlik sorgusu; rezervasyonda ikinci doğrulama
- Mobil uyumlu misafir paneli ve yönetim panelinden bağımsız tasarım

### Demo içerik

`SEED_DEMO_DATA=true` ile ilk başlatmada ya da v1 kurulumuna güncelleme yapıldığında (var olan kayıtlar silinmeden) eksik kayıtlar eklenir:

| Katalog | Örnek adedi |
|---|---:|
| Fiziksel ekipman | 48 |
| Konaklama birimi | 18 |
| Departman | 9 |
| Personel (çalışan kaydı) | 16 |
| Müşteri (iletişim kaydı) | 18 |
| Pist | 12 |
| Ders / etkinlik | 16 |
| Bakım kayıtları | En fazla 4 |
| Örnek kiralama | En fazla 5 |
| Örnek konaklama | En fazla 5 |
| Örnek ders kaydı | En fazla 8 |
| Örnek tahsilat | En fazla 8 |

Bu sayılar hedef demo tanımlarını gösterir. İlgili kod/ad zaten varsa tekrar eklenmez; gerçek müşterilere ait çakışan rezervasyonların yerine demo rezervasyon üretilmez. Önceki sürümden kalan farklı örnek kayıtlar da korunur. Demo müşteri e-postaları `.test` uzantılıdır ve **hiçbirinin giriş parolası yoktur**. Paneli denemek için kendin yeni hesap aç.

## Mevcut v1 kurulumunu güncelleme

> Önce PostgreSQL yedeğini al. Yeni sürümü mevcut proje klasörüne doğrudan kopyalamadan önce yedeğini sakla.

1. `docker compose ps` ile çalışan servislerini gör.
2. Önceki projenin `docker compose` çalıştırıldığı klasörde, örneğin PowerShell ile şu yedeği oluştur (kullanıcı/DB ismi `.env` ile aynı olmalı):

   ```powershell
   docker compose exec -T db sh -c 'pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc -f /tmp/skiresort-before-v2.dump'
   docker compose cp db:/tmp/skiresort-before-v2.dump ./skiresort-before-v2.dump
   ```

   Bu komut dump dosyasını önce konteyner içinde oluşturur, sonra ikili dosyayı bozmadan kopyalar. Restore prova etmeden yedeğe güvenme.

3. Yeni ZIP'i **ayrı bir klasöre** aç. `compose.yaml` içindeki proje adı/volume adlandırma davranışına dikkat et: yeni klasörde çalıştırılan Compose yeni bir DB volume oluşturabilir; eski verilerin görünmemesi silindikleri anlamına gelmez.
4. En kolay ve güvenli yol: önceki proje klasörünü yedekleyip **v2 kaynak dosyalarını aynı proje klasörüne** kopyala; önceki `.env` dosyasını olduğu gibi koru. Docker volume'u saklanır.
5. `.env` dosyasında **yerel geliştirme ortamı** için `SEED_DEMO_DATA=true` değerini kontrol et. Gerçek verili/üretim ortamında `false` kullan.
6. `docker compose up -d --build` ve `docker compose logs --tail=100 api` ile yeni sürümü çalıştır.
7. Yönetici girişinin ve misafir girişinin açıldığını doğrula.

Yüklenme sırasında eski veritabanı olan kurulumlarda `Customers` tablosuna dört alan (`PasswordHash`, `IsActive`, `FailedAttempts`, `LockedUntil`) eklenir. Bu, EF migrations yerine **belgelenmiş geçici bir additive yükseltmedir**; canlı üretim için versioned migrations'a dönüştürülmelidir.

## Mimari ve yetkilendirme

| İşlem | Admin / Yetkili personel | Müşteri |
|---|---|---|
| Tüm müşteri listesi / personel / finans yönetimi | Rolüne göre izin | **403** |
| Kendi profili | Personel hesabı üzerinden | Erişim var |
| Kendi kiralama / konaklama geçmişi | İşletme kayıtları | Erişim var |
| Başkasının rezervasyonunu görme/iptal | Yetkili rol | **Yok (404)** |
| Ekipman ve oda rezervasyonu | İşletme rezervasyonu | Kendi adına |
| Ekipman teslim/tesellüm, otel check-in | İşletme personeli | Erişim yok |
| Gerçek kredi kartı ile ödeme | **Henüz yok** | **Henüz yok** |

Müşteri ID'si API request body'den alınmaz, doğrulanmış cookie session içerisindeki claim'den elde edilir. Müşteri rotaları `Customer`, işletme rotaları `Staff`/ilgili operasyon rolü politikasını kullanır. Anti-CSRF, HttpOnly cookie, login rate limiting, parola hash'i, PostgreSQL çakışma kısıtları ve iş kuralları korunur.

## Önemli eksikler ve kapsam

- Gerçek e-posta doğrulama, şifre sıfırlama, KVKK saklama/silme süreçleri, gerçek online ödeme ve faturalandırma yoktur.
- Daha önce **sadece personel tarafından oluşturulmuş** e-posta kontakları, hesabın sahibi olduğuna dair doğrulama yapılmadan parolayla talep edilemez; eşleşen e-posta için tesise başvurma mesajı çıkar. Bu güvenlik içindir.
- Müşteri rezervasyon iptali yalnızca gelecekte başlayan, henüz `Reserved` durumunda ve ödeme kaydı olmayan işlemlerde mümkündür. Parası tahsil edilmiş bir rezervasyonda personel ile görüşülür; otomatik para iadesi yapılmaz.
- Demo tahsilat kayıtları gerçek finans hareketi değildir; yalnızca geliştirme ortamı içindir.
- Katalogdaki anlık uygunluk bilgisi bilgilendirme amaçlıdır; iki kullanıcı aynı anda rezervasyon yaptığında nihai karar veritabanının exclusion constraint'ine aittir.
- Bu teslimat kaynağın güncellenmiş halidir. .NET SDK ve Docker'ın olmadığı hazırlanma ortamında gerçek servis çalıştırma ve tarayıcı E2E testleri geçildi olarak beyan edilemez.

## Manuel kontrol senaryoları

1. Admin ile giriş yap → yeni veri sayılarını kontrol et (demo açık ise).
2. Çıkış yap → Misafir Paneline Geç → yeni e-postayla hesap aç.
3. Ekipman seç → uygun tarihlerde rezervasyon oluştur → Rezervasyonlarım'da gör.
4. Admin ile giriş yap → aynı rezervasyonu Ekipman Kiralama ekranında gör.
5. Misafir ile oda rezervasyonu ve ders kaydı yap, admin panelinden doğrula.
6. Misafir olarak yönetici panelinin API `/api/dashboard` ve `/api/customers` yollarına erişmeyi dene → 403.
7. İkinci müşteri hesabıyla ilk müşterinin kayıtlarını görüntüle / iptal et → yasak.
8. Çakışan tarihte aynı ekipman/oda rezervasyonu → 409.
9. Ödeme kaydı admin tarafından eklenirse yalnızca ilgili misafirin ödemelerinde görünür.
10. `docker compose down` ve `up -d` ile volume kalıcılığını doğrula (**`down -v` kullanma**).

Otomatik test: `python scripts/customer_integration_test.py` yalnızca ayrı ve **silinmesi sorun olmayan** bir demo veritabanında, `I_CONFIRM_DISPOSABLE_DB=YES` ayarlanarak çalıştırılmalıdır.
