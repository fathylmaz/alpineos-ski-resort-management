# HTTP API referansı

Adres: `/api`. Durum değiştiren isteklerde önce `GET /api/auth/csrf` çağırın ve token değerini `X-CSRF-TOKEN` header'ı olarak gönderin. Giriş sonrası CSRF tokenını yeniden alın. Çerezleri aynı kök alan adına gönderin.

| Metot | URL | Amaç | Yazma rolü |
|---|---|---|---|
| GET | `/health` | Basit uygulama sağlığı | Açık |
| GET | `/auth/csrf` | CSRF tokenı | Açık |
| POST | `/auth/login` | Personel girişi | Açık, CSRF gerekli |
| POST | `/auth/logout` | Oturumu kapat | Oturum |
| GET | `/auth/me` | Oturum bilgisi | Oturum |
| GET / POST | `/auth/staff` | Hesap listesi / hesap aç | Admin |
| GET / POST / PUT | `/customers` / `/customers/{id}` | Müşteri liste/ekle/güncelle | Reception / Admin |
| GET / POST | `/equipment` | Ekipman liste/ekle | Equipment / Admin |
| PATCH | `/equipment/{id}/status` | Ekipman durumu | Equipment / Admin |
| GET / POST | `/equipment/{id}/maintenance` | Bakım kaydı | Equipment / Admin |
| GET / POST | `/rooms` | Konaklama birimi | Reception / Admin |
| GET / POST | `/rentals` | Kiralama liste/rezervasyon | Reception / Admin |
| POST | `/rentals/{id}/checkout` | Teslim et | Reception / Admin |
| POST | `/rentals/{id}/return` | İade al | Reception / Admin |
| POST | `/rentals/{id}/cancel` | İptal | Reception / Admin |
| GET / POST | `/stays` | Konaklama liste/rezervasyon | Reception / Admin |
| POST | `/stays/{id}/checkin` | Misafir girişi | Reception / Admin |
| POST | `/stays/{id}/checkout` | Misafir çıkışı | Reception / Admin |
| POST | `/stays/{id}/cancel` | Rezervasyon iptali | Reception / Admin |
| GET / POST | `/slopes` | Pist liste/ekle | Slope / Admin |
| PATCH | `/slopes/{id}/status` | Pist durum güncelle | Slope / Admin |
| GET / POST | `/activities` | Ders ve etkinlik | Reception / Admin |
| GET | `/activities/{id}/enrollments` | Katılımcı listesi | Oturum |
| POST | `/activities/{id}/enroll` | Katılımcı ekle | Reception / Admin |
| POST | `/activities/{id}/enrollments/{customerId}/cancel` | Katılım iptal | Reception / Admin |
| GET / POST | `/payments` | Tahsilat ve iade kaydı | Cashier / Admin |
| GET | `/dashboard` | Göstergeler | Oturum |
| GET | `/dashboard/audit` | Son 100 denetim kaydı | Admin |
| GET / POST | `/departments` | Departman | Admin |
| GET / POST | `/employees` | Personel | Admin |

## Örnekler

Kiralama oluşturma (`POST /api/rentals`):

```json
{
  "customerId":"00000000-0000-0000-0000-000000000001",
  "equipmentId":"00000000-0000-0000-0000-000000000002",
  "startAt":"2027-01-05T09:00:00Z",
  "endAt":"2027-01-07T09:00:00Z"
}
```

Örnek ID'ler temsili; listeden gerçek GUID kullanılmalı.

Konaklama oluşturma (`POST /api/stays`):

```json
{
  "customerId":"00000000-0000-0000-0000-000000000001",
  "roomId":"00000000-0000-0000-0000-000000000003",
  "startAt":"2027-01-05T00:00:00Z",
  "endAt":"2027-01-08T00:00:00Z",
  "guestCount":2
}
```

Finans işlemi (`POST /api/payments`):

```json
{
  "referenceType":"Rental",
  "referenceId":"00000000-0000-0000-0000-000000000004",
  "kind":"Charge",
  "method":"Cash",
  "amount":300,
  "externalReference":"MANUAL-0001"
}
```

Tüm fiyatlandırma backend'de gerçekleşir. Hata kodları: `400` geçersiz veri / CSRF, `401` kimlik, `403` yetki, `404` kayıt yok, `409` durum/benzersizlik/tarih çakışması, `429` giriş rate limit, `500` beklenmedik sunucu hatası.
