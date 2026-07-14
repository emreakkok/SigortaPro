# SigortaPro — API Uç Nokta Özeti

> Tüm uçların canlı ve ayrıntılı dokümantasyonu (istek/yanıt şemaları dahil) Development ortamında **Swagger** üzerindedir: `http://localhost:5153/swagger`. Bu doküman hızlı başvuru amaçlı özet niteliğindedir.

## Genel Sözleşme

- **Base URL:** `/api/v1` (URL tabanlı versiyonlama — ADR-019)
- **Kimlik doğrulama:** `Authorization: Bearer {accessToken}` (JWT — access 15 dk, refresh 7 gün rotasyonlu; ADR-003)
- **Hata formatı:** RFC 7807 `ProblemDetails` (`application/problem+json`); her yanıtta `traceId` + `correlationId`. Doğrulama hatalarında alan bazlı `errors` sözlüğü döner (ADR-018). Auth uçlarının _beklenen_ hataları (yanlış şifre, duplicate e-posta) `{ "errors": ["..."] }` gövdesiyle döner.
- **Sayfalama:** Listeleme uçları `page` (varsayılan 1) + `pageSize` (varsayılan 20, en fazla 100) alır ve `{ items, page, pageSize, totalCount, totalPages }` zarfı döner.
- **Rate limit:** `auth/*` uçlarında IP başına dakikada 10 istek; aşımda `429` (ADR-020).
- **Correlation:** Her istek `X-Correlation-ID` ile ilişkilendirilir (gelen header korunur, yoksa üretilir; yanıt header'ında döner).

### Enum Sözleşmesi (sayısal — ADR-030)

Enum'lar JSON'da **sayısal indeks** olarak serileştirilir ve query/body'de sayısal beklenir:

| Enum | Değerler |
|------|----------|
| `InsuranceBranch` | 0 Kasko · 1 Trafik · 2 Konut · 3 Dask · 4 Saglik |
| `QuoteStatus` | 0 Draft · 1 Priced · 2 Approved · 3 Purchased · 4 Expired · 5 Rejected |
| `PolicyStatus` | 0 Active · 1 Expired · 2 Cancelled |
| `ClaimStatus` | 0 Submitted · 1 UnderReview · 2 Approved · 3 Rejected · 4 Paid |
| `PaymentStatus` | 0 Pending · 1 Successful · 2 Failed |
| `CoveragePackage` | 0 Standart · 1 Genisletilmis · 2 Premium |
| `RiskScore` | 0 Low · 1 Medium · 2 High |

### Roller

`Customer` (müşteri — yalnızca kendi verisi), `Admin` ve `Personel` (birlikte **Staff** — tüm kayıtlar, kaynak sahipliğinden muaf).

---

## Kimlik Doğrulama — `/api/v1/auth` (Task 5)

| Metot | Uç | Açıklama | Yetki |
|-------|----|----------|-------|
| `POST` | `/auth/register` | Müşteri kaydı (Identity kullanıcısı + Customer profili, atomik) → oturum | Anonim |
| `POST` | `/auth/login` | Giriş → access + refresh token (`401` yanlış kimlik) | Anonim |
| `POST` | `/auth/refresh-token` | Rotasyonlu token yenileme (eski refresh revoke edilir; `401` geçersiz/kullanılmış token) | Anonim |

## Müşteri & Profil — `/api/v1/customers` (Task 7)

| Metot | Uç | Açıklama | Yetki |
|-------|----|----------|-------|
| `GET` | `/customers/me` | Oturum sahibinin profili (araç/konut risk objeleriyle; TCKN maskeli) | `Customer` |
| `PUT` | `/customers/me` | Ad/soyad, telefon, adres güncelleme (TCKN/doğum tarihi değişmez) | `Customer` |
| `POST` | `/customers/me/vehicles` | Araç ekleme | `Customer` |
| `PUT` | `/customers/me/vehicles/{vehicleId}` | Araç güncelleme (sahiplik kontrollü → `403`) | `Customer` |
| `POST` | `/customers/me/properties` | Konut ekleme | `Customer` |
| `GET` | `/customers?searchTerm=&city=&page=&pageSize=` | Müşteri listesi (ad/soyad/TCKN araması + il filtresi) | `Staff` |
| `GET` | `/customers/{id}` | Müşteri detayı | `Staff` |

## Teklif — `/api/v1/quotes` (Task 9)

Teklif durum makinesi: `Draft → Priced → Approved → Purchased`, ara durumlardan `Expired`/`Rejected`. Geçersiz geçiş → `409`.

| Metot | Uç | Açıklama | Yetki |
|-------|----|----------|-------|
| `POST` | `/quotes` | Branş + risk objesi (`vehicleId`/`propertyId`; Sağlık'ta gerekmez) + teminat paketi → fiyatlanmış teklif (`201`) | `Customer` |
| `GET` | `/quotes/compare?branch=&vehicleId=&propertyId=` | 3 teminat paketi alternatifinin önizlemesi (teklif oluşturmaz) | `Customer` |
| `GET` | `/quotes?status=&branch=&page=&pageSize=` | Teklif listesi (müşteri kendi / personel tümü) | Kimliği doğrulanmış |
| `GET` | `/quotes/{id}` | Teklif detayı (prim dökümü + teminatlar; deterministik yeniden hesap — ADR-021) | Sahip müşteri / `Staff` |
| `POST` | `/quotes/{id}/approve` | Priced → Approved (geçerlilik süresi dolmuşsa `409`) | `Customer` |
| `POST` | `/quotes/{id}/reject` | → Rejected | `Customer` |

## Ödeme — `/api/v1/payments` (Task 10)

| Metot | Uç | Açıklama | Yetki |
|-------|----|----------|-------|
| `POST` | `/payments` | Onaylı teklifi mock POS ile öde → `Payment` + aktif `Policy` + teklif `Purchased` (tek transaction — ADR-022). Ödeme reddi → `402`; onaysız teklif → `409` | `Customer` |
| `GET` | `/payments?page=&pageSize=` | Ödeme geçmişi (başarılı/başarısız; maskeli kart) | `Customer` |
| `GET` | `/payments/installment-options?quoteId=` | Onaylı teklifin taksit seçenekleri (1/3/6/9/12, faizsiz mock) | `Customer` |

> Test kartları ve senaryoları için bkz. [README — Mock Sanal POS Test Kartları](README.md#mock-sanal-pos-test-kartları-yalnızca-geliştirme).

## Poliçe — `/api/v1/policies` (Task 11, 18)

| Metot | Uç | Açıklama | Yetki |
|-------|----|----------|-------|
| `GET` | `/policies?status=&page=&pageSize=` | "Poliçelerim": oturum sahibinin poliçeleri (durum filtresi) | `Customer` |
| `GET` | `/policies/{id}` | Poliçe detayı (teminat tablosu ile; ADR-021 deterministik) | Sahip müşteri / `Staff` |
| `GET` | `/policies/{id}/document` | Poliçe sertifikası PDF'i (`application/pdf`; ilk erişimde üretilir — ADR-023) | Sahip müşteri / `Staff` |

## Hasar — `/api/v1/claims` (Task 12)

Hasar durum makinesi: `Submitted → UnderReview → Approved → Paid`, incelemeden `Rejected`. Geçersiz geçiş → `409`.

| Metot | Uç | Açıklama | Yetki |
|-------|----|----------|-------|
| `POST` | `/claims` | Hasar bildirimi (yalnızca aktif poliçe + dönem içi olay → aksi `409`; foto adları mock metadata — ADR-024) | `Customer` |
| `GET` | `/claims?status=&policyId=&page=&pageSize=` | Hasar listesi (müşteri kendi / personel tümü) | Kimliği doğrulanmış |
| `GET` | `/claims/{id}` | Hasar detayı (sahiplik kontrollü) | Sahip müşteri / `Staff` |
| `POST` | `/claims/{id}/start-review` | Submitted → UnderReview | `Staff` |
| `POST` | `/claims/{id}/approve` | UnderReview → Approved (onay tutarı + opsiyonel not) | `Staff` |
| `POST` | `/claims/{id}/reject` | UnderReview → Rejected (gerekçe zorunlu) | `Staff` |
| `POST` | `/claims/{id}/pay` | Approved → Paid | `Staff` |

## Yenileme — `/api/v1/renewals` (Task 13)

Yenileme teklifleri arkaplan servisince üretilir (bitişe ≤30 gün; güncel fiyatlama × hasar geçmişi çarpanı — ADR-025).

| Metot | Uç | Açıklama | Yetki |
|-------|----|----------|-------|
| `GET` | `/renewals?page=&pageSize=` | Müşterinin yenileme teklifleri | `Customer` |
| `POST` | `/renewals/{id}/accept` | Yenilemeyi onayla → yeni dönem teklifi `Approved` (ödeme mevcut `POST /payments` ile; çift onay `409`) | `Customer` |

## Dashboard & Raporlama — `/api/v1/dashboard` (Task 14)

Tümü salt okunur, `Staff` yetkili (ADR-026). Rapor uçlarında `from`/`to` **dahil** tarih aralığıdır; `to < from` → `400`.

| Metot | Uç | Açıklama | Yetki |
|-------|----|----------|-------|
| `GET` | `/dashboard/summary` | Özet metrikler + oranlar (0–1 ondalık) + aylık trend (son 12 ay) + branş dağılımı | `Staff` |
| `GET` | `/dashboard/reports/policies?from=&to=&page=&pageSize=` | Tarih aralıklı poliçe raporu (başlangıç tarihine göre) | `Staff` |
| `GET` | `/dashboard/reports/payments?from=&to=&page=&pageSize=` | Tarih aralıklı ödeme raporu (işlem tarihine göre) | `Staff` |
| `GET` | `/dashboard/reports/riskiest-customers?top=` | Hasar sayısına göre en riskli müşteriler (varsayılan ilk 10) | `Staff` |

## Sistem

| Metot | Uç | Açıklama | Yetki |
|-------|----|----------|-------|
| `GET` | `/health` | Veritabanı bağlantısını yoklayan JSON sağlık yanıtı | Anonim |
| `GET` | `/swagger` | OpenAPI dokümantasyonu + JWT "Authorize" (yalnızca Development) | Anonim |

---

## HTTP Durum Kodları (özet — CODING_STANDARDS.md §5.5)

| Kod | Kullanım |
|-----|----------|
| `200` / `201` | Başarılı okuma-güncelleme / oluşturma |
| `400` | Doğrulama hatası (alan bazlı `errors` ile) |
| `401` | Kimlik doğrulama başarısız / token geçersiz |
| `402` | Ödeme reddi (yetersiz bakiye, 3D hata, geçersiz kart) |
| `403` | Kaynak sahipliği ihlali / rol yetkisizliği |
| `404` | Kaynak bulunamadı |
| `409` | İş kuralı ihlali / geçersiz durum geçişi / duplicate |
| `429` | Rate limit aşıldı (auth uçları) |
| `500` | Beklenmeyen hata (detay yalnızca Development'ta) |
