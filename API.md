# SigortaPro — API Uç Nokta Özeti

> Tüm uçların canlı ve ayrıntılı dokümantasyonu (istek/yanıt şemaları dahil) Development ortamında **Swagger** üzerindedir: `http://localhost:5153/swagger`. Bu doküman hızlı başvuru amaçlı özet niteliğindedir.

## Genel Sözleşme

- **Base URL:** `/api/v1` (URL tabanlı versiyonlama — ADR-019)
- **Kimlik doğrulama:** `Authorization: Bearer {accessToken}` (JWT — access 15 dk, refresh 7 gün rotasyonlu; ADR-003)
- **Hata formatı:** RFC 7807 `ProblemDetails` (`application/problem+json`); her yanıtta `traceId` + `correlationId`. Doğrulama hatalarında alan bazlı `errors` sözlüğü döner (ADR-018). Auth uçlarının _beklenen_ hataları (yanlış şifre, duplicate e-posta) `{ "errors": ["..."] }` gövdesiyle döner.
- **Sayfalama:** Listeleme uçları `page` (varsayılan 1) + `pageSize` (varsayılan 20, en fazla 100) alır ve `{ items, page, pageSize, totalCount, totalPages }` zarfı döner.
- **Rate limit:** `auth/*` uçlarında IP başına dakikada 10 istek; aşımda `429` (ADR-020).
- **Correlation:** Her istek `X-Correlation-ID` ile ilişkilendirilir (gelen header korunur, yoksa üretilir; yanıt header'ında döner).
- **Tarih-saat (ADR-063):** **Instant** alanlar (olay/oluşturma/ödeme/bildirim zamanları vb.) UTC olarak saklanır ve **ISO-8601 + "Z"** ile döner (ör. `"2026-07-26T10:23:00Z"`); istemci gönderirken de UTC "Z" göndermelidir. Sunum katmanı (frontend) bunu **Europe/Istanbul** yerel saatine çevirir. **Date-only** alanlar (ör. doğum tarihi) takvim günüdür, timezone dönüşümüne tabi değildir ve "Z" içermez.

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
| `VehicleUsage` | 0 Hususi · 1 Ticari · 2 Taksi |
| `RiskScore` | 0 Low · 1 Medium · 2 High |

### Roller

`Customer` (müşteri — yalnızca kendi verisi), `Admin` ve `Personel` (birlikte **Staff** — tüm kayıtlar, kaynak sahipliğinden muaf).

**Admin ⊃ Personel ayrımı (ADR-060):** `Staff` ortak operasyon yüzeyidir (müşteri/teklif/poliçe/hasar inceleme + onay/ret, dashboard özet/poliçe/risk raporları). Şu yüzeyler **yalnızca `Admin`**'e açıktır: personel yönetimi (`/staff/*`), fiyatlandırma (`/pricing/*`), **hasar ödeme** (`/claims/{id}/pay`) ve **ödeme/ciro raporu** (`/dashboard/reports/payments`). `Personel` bu uçlarda `403` alır.

---

## Kimlik Doğrulama — `/api/v1/auth` (Task 5)

| Metot | Uç | Açıklama | Yetki |
|-------|----|----------|-------|
| `POST` | `/auth/register` | Müşteri kaydı (Identity kullanıcısı + Customer profili, atomik) → oturum | Anonim |
| `POST` | `/auth/login` | Giriş → access + refresh token (`401` yanlış kimlik) | Anonim |
| `POST` | `/auth/refresh-token` | Rotasyonlu token yenileme (eski refresh revoke edilir; `401` geçersiz/kullanılmış token) | Anonim |
| `POST` | `/auth/forgot-password` | Şifre sıfırlama talebi → kayıtlı e-postaya bağlantı gönderir. Güvenlik gereği e-posta kayıtlı olsun/olmasın **her zaman `200`** (enumeration koruması — ADR-035) | Anonim |
| `POST` | `/auth/reset-password` | Token + yeni şifre ile şifreyi günceller (`400` token geçersiz/süresi dolmuş; token ömrü 1 saat) | Anonim |
| `POST` | `/auth/change-password` | Oturum sahibinin şifresini değiştirir (mevcut şifre doğrulamalı; `400` mevcut şifre hatalı — ADR-040) | Kimliği doğrulanmış |

> Tüm `auth/*` uçları IP başına 10 istek/dk rate limit'lidir (ADR-020). Şifre sıfırlama e-postası, sağlayıcıdan bağımsız `IEmailService` (MVP: SMTP/MailKit) üzerinden gönderilir; SMTP yapılandırması `dotnet user-secrets`/ortam değişkeni ile sağlanır (bkz. README).

## Müşteri & Profil — `/api/v1/customers` (Task 7)

| Metot | Uç | Açıklama | Yetki |
|-------|----|----------|-------|
| `GET` | `/customers/me` | Oturum sahibinin profili (araç/konut risk objeleriyle; TCKN maskeli) | `Customer` |
| `PUT` | `/customers/me` | Ad/soyad, telefon, adres güncelleme (TCKN/doğum tarihi değişmez) | `Customer` |
| `POST` | `/customers/me/vehicles` | Araç ekleme. **`usagePurpose` zorunludur** (0 Hususi · 1 Ticari · 2 Taksi — ADR-057); Kasko/Trafik primini etkiler, varsayılan atanmaz | `Customer` |
| `PUT` | `/customers/me/vehicles/{vehicleId}` | Araç güncelleme (sahiplik kontrollü → `403`). `usagePurpose` zorunludur; güncelleme **yalnızca yeni teklifleri** etkiler (mevcut teklifler girdiyi snapshot'lar) | `Customer` |
| `POST` | `/customers/me/properties` | Konut ekleme. `earthquakeZone` **istekte YOKTUR** — sistem adresin ilinden türetir (ADR-055/058); istemci gönderse bile yok sayılır. Konut güncelleme ucu bulunmadığından bölge sonradan değiştirilemez | `Customer` |
| `GET` | `/customers?searchTerm=&city=&page=&pageSize=` | Müşteri listesi — `searchTerm` ad/soyad/TCKN/**e-posta/telefon** eşler (telefon normalize: boşluk/parantez/tire ve baştaki 0/90 yok sayılır — ADR-040) + il filtresi | `Staff` |
| `GET` | `/customers/{id}` | Müşteri detayı | `Staff` |

## Araç Katalogu — `/api/v1/vehicle-catalog` (Task 24)

| Metot | Uç | Açıklama | Yetki |
|-------|----|----------|-------|
| `GET` | `/vehicle-catalog` | Araç marka/model kataloğu (cascading select verisi; gömülü JSON, In-Memory cache — ADR-036) | Kimliği doğrulanmış |

> Salt okunur referans veridir; `{ brands: [ { name, models: [...] } ] }` döner. Frontend `VehicleForm` bu veriyle marka→model aranabilir combobox sunar; listede olmayan araçlar "Diğer" ile serbest metin girilir. `Vehicle.Brand`/`Model` alan tipleri (`string`) ve araç ekleme/güncelleme sözleşmeleri değişmemiştir.

## İl Katalogu — `/api/v1/city-catalog` (Post-MVP, ADR-037)

| Metot | Uç | Açıklama | Yetki |
|-------|----|----------|-------|
| `GET` | `/city-catalog` | Türkiye'nin 81 ili (adres formu combobox verisi; gömülü JSON, In-Memory cache — ADR-037) | Anonim (kayıt formu da tüketir — ADR-039) |

> Salt okunur referans veridir; `{ cities: [ { name }, ... ] }` döner (nesne şekli, ileride ilçe desteği için additive). Adres formları (kayıt/profil/konut) bu veriyle aranabilir il seçici sunar. `Address.City` alan tipi (`string`) ve adres içeren sözleşmeler (`register`, `UpdateProfile`, `AddProperty`) değişmemiştir.

## Teklif — `/api/v1/quotes` (Task 9)

Teklif durum makinesi: `Draft → Priced → Approved → Purchased`, ara durumlardan `Expired`/`Rejected`. Geçersiz geçiş → `409`.

| Metot | Uç | Açıklama | Yetki |
|-------|----|----------|-------|
| `POST` | `/quotes` | Branş + risk objesi (`vehicleId`/`propertyId`; Sağlık'ta gerekmez) + teminat paketi → fiyatlanmış teklif (`201`). Sağlıkta opsiyonel `insuredPerson` ile **başkası adına** teklif (ADR-041; TCKN maskeli döner). Sağlıkta **`isSmoker` zorunludur** (ADR-054) — beyan yoksa `400`; diğer branşlarda gönderilirse `400` | `Customer` |
| `GET` | `/quotes/compare?branch=&vehicleId=&propertyId=&insuredBirthDate=&isSmoker=` | 3 teminat paketi alternatifinin önizlemesi (teklif oluşturmaz). **Kuralları `POST /quotes` ile birebir aynıdır** (ADR-056): Sağlıkta `isSmoker` zorunlu (yoksa `400`), diğer branşlarda gönderilemez. Böylece burada gösterilen prim, aynı seçimle oluşturulacak teklifin primiyle **yapısal olarak eşittir** | `Customer` |
| `GET` | `/quotes?status=&branch=&search=&page=&pageSize=` | Teklif listesi (müşteri kendi / personel tümü). `search`: müşteri adı/soyadı/tam adı veya telefon (format bağımsız — ADR-051). Özet artık `customerId`/`customerFullName`/`customerPhone` taşır (müşteri kapsamı korunur; sızıntı yok) | Kimliği doğrulanmış |
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

> **Fiyatlama girdileri teklifte dondurulur (ADR-053):** Teklif oluşturulurken motora giden tüm primitifler (yaş, araç yaşı, motor gücü, risk ili, bina yaşı, m², deprem bölgesi, sigara beyanı) `Quote` üzerinde saklanır. Teklif detayı, poliçe detayı ve PDF **yalnızca bu dondurulmuş girdilerden** yeniden hesaplanır → müşteri profilini/aracını sonradan değiştirse bile **eski teklifin primi ve prim dökümü değişmez**. Ayrıca gerçek veriye dayanmayan faktörler dökümde **gösterilmez** (ADR-054), ve `POST /customers/me/properties` artık `earthquakeZone` **almaz** — bölge adresin ilinden türetilir (ADR-055).

> **`incidentDate` saat taşır (ADR-050):** Olay anı **tarih + saat** olarak alınır ve teminat penceresi **saat hassasiyetinde** doğrulanır: `StartDate ≤ incidentDate ≤ EndDate` (sınırlar dahil). `StartDate`/`EndDate` poliçenin satın alma anını (UTC) taşıdığından, **aynı gün poliçe başlangıç saatinden sonra** gerçekleşen olay geçerlidir; **öncesi** geçersizdir. Frontend olay tarih+saatini birleştirip UTC ISO gönderir; yalnızca tarih (gece yarısı) göndermek aynı gün hasarını yanlışlıkla reddederdi.
| `GET` | `/claims?status=&policyId=&page=&pageSize=` | Hasar listesi (müşteri kendi / personel tümü) | Kimliği doğrulanmış |
| `GET` | `/claims/{id}` | Hasar detayı (sahiplik kontrollü) | Sahip müşteri / `Staff` |
| `POST` | `/claims/{id}/start-review` | Submitted → UnderReview | `Staff` |
| `POST` | `/claims/{id}/approve` | UnderReview → Approved (onay tutarı + opsiyonel not) | `Staff` |
| `POST` | `/claims/{id}/reject` | UnderReview → Rejected (gerekçe zorunlu) | `Staff` |
| `POST` | `/claims/{id}/pay` | Approved → Paid | **Yalnızca Admin** (ADR-060: görevler ayrılığı — para çıkışı Personel'e kapalı) |

## Yenileme — `/api/v1/renewals` (Task 13)

Yenileme teklifleri arkaplan servisince üretilir (bitişe ≤30 gün; güncel fiyatlama × hasar geçmişi çarpanı — ADR-025).

| Metot | Uç | Açıklama | Yetki |
|-------|----|----------|-------|
| `GET` | `/renewals?page=&pageSize=` | Müşterinin yenileme teklifleri | `Customer` |
| `POST` | `/renewals/{id}/accept` | Yenilemeyi onayla → yeni dönem teklifi `Approved` (ödeme mevcut `POST /payments` ile; çift onay `409`) | `Customer` |

## Dashboard & Raporlama — `/api/v1/dashboard` (Task 14)

Rapor uçlarında `from`/`to` **dahil** tarih aralığıdır; `to < from` → `400`.

| Metot | Uç | Açıklama | Yetki |
|-------|----|----------|-------|
| `GET` | `/dashboard/summary?from=&to=` | **Operasyon dashboard'ının tüm blokları tek çağrıda** (ADR-052): seçilen aralığın KPI'ları (`current`) + önceki **eşit uzunluktaki** dönem (`previous`) ve oransal değişim (`deltas`), aksiyon merkezi (`alerts`), portföy, satış hunisi (`funnel`), prim üretimi zaman serisi (`premiumSeries`), branş performansı, hasar operasyonu (`claims`), dönemsel `renewalRate`. Aralık verilmezse **son 30 gün**; ters/aşırı uzun aralık `400` | `Staff` (rol bazlı finansal maskeleme — bkz. aşağıdaki not, ADR-062) |

> **Rol bazlı finansal görünürlük (ADR-062):** `/dashboard/summary` **tek endpoint** olarak kalır ve `Staff`'a açıktır; ancak **agregat finansal alanlar** çağıranın rolüne göre backend'de maskelenir. **Personel** için şu alanlar `null` döner (veri response'a yazılmaz): `current/previous/deltas.premiumProduction`, `alerts.failedPayments`, `portfolio.lifetimePremiumProduction/paidClaimAmount/lossRatio`, `premiumSeries[].premiumTotal`, `branchPerformance[].premiumTotal`, `claims.paidAmount/estimatedAmount`. **Operasyonel alanlar** (adetler, aksiyon merkezi iş yükü, portföy adetleri, huni, hasar durum kırılımı, yenileme oranı, branş adet/dönüşüm) Personel için **dolu** gelir. **Admin** finansal + operasyonel alanların **tamamını** görür. Frontend, Personel için finansal kartları/sütunları hiç render etmez (yalnızca frontend gizleme güvenlik sayılmaz — asıl kısıt backend maskelemesidir).

> **Dashboard veri sözleşmesi (ADR-052):** Oranlar 0–1 ondalıktır ve **payda 0 iken `null`** döner (dönüşüm, yenileme, hasar/prim, delta) — "%0" veya "+%100" gibi yanıltıcı değer üretilmez. `granularity` (0 Hourly · 1 Daily · 2 Monthly) aralık uzunluğundan türetilir. Prim serisi **`Policy.CreatedAt`** (üretim tarihi) bazlıdır; poliçe raporu ise `StartDate` (teminat başlangıcı) bazlıdır — bilinçli farktır. Satış hunisi, dönemde oluşturulan tekliflerin kohortudur; `Draft` kalıcı olmadığından huni "Fiyatlandı"dan başlar ve `approved` satın alınanları da içerir (monoton azalır).
| `GET` | `/dashboard/reports/policies?from=&to=&search=&page=&pageSize=` | Tarih aralıklı poliçe raporu (başlangıç tarihine göre). `search`: müşteri adı/soyadı/tam adı, telefon (format bağımsız) veya poliçe numarası. Kalem artık `customerId`/`customerPhone` taşır (ADR-051). **Kayıt-başına poliçe primi (`totalPremium`) Personel'e açıktır** (ADR-062 D2 — operasyoneldir) | `Staff` |
| `GET` | `/dashboard/reports/payments?from=&to=&page=&pageSize=` | Tarih aralıklı ödeme/ciro raporu (işlem tarihine göre) | **Yalnızca Admin** (ADR-060: ciro görünürlüğü yönetimseldir) |
| `GET` | `/dashboard/reports/riskiest-customers?top=` | Hasar sayısına göre en riskli müşteriler (varsayılan ilk 10) | **Yalnızca Admin** (ADR-062 D3: hasar tutarı + müşteri profilleme yönetimsel/KVKK) |

## Bildirimler — `/api/v1/notifications` (ADR-042)

Kalıcı bildirim merkezi. Tüm uçlar oturum sahibinin **kendi** bildirimleriyle sınırlıdır (alıcı bazlı model); üretim şu an staff kitlesine yapılır, müşteri alıcılığı hazırdır.

| Metot | Uç | Açıklama | Yetki |
|-------|----|----------|-------|
| `GET` | `/notifications?isRead=&severity=&searchTerm=&from=&to=&page=&pageSize=` | Bildirim geçmişi (en yeni önce; okunma/önem/metin/tarih filtreleri + sayfalama) | Kimliği doğrulanmış |
| `GET` | `/notifications/unread-count` | Okunmamış bildirim sayısı (zil rozeti) | Kimliği doğrulanmış |
| `POST` | `/notifications/{id}/read` | Tek bildirimi okundu işaretler (yalnızca alıcısı — aksi `403`) | Kimliği doğrulanmış |
| `POST` | `/notifications/read-all` | Tüm okunmamışları okundu işaretler | Kimliği doğrulanmış |

> `severity`: `success` · `info` · `warning` · `error`. Canlı iletim SignalR hub'ı üzerinden yapılır (aşağıda); geçmiş her zaman bu uçlardan (DB) okunur.
>
> **ADR-047 (additive):** Bildirim kaydı operasyonel bağlam taşır — `actorName` (işlemi yapanın olay anındaki görünen adı; snapshot) ve `referenceCode` (gerçek poliçe numarası; teklif/hasar numarası veri modelinde bulunmadığından `null`). `relatedEntityType` + `relatedEntityId` ilgili kayda gitmek için kullanılır (`Quote` · `Policy` · `Claim` · `Customer`); admin arayüzünde derin bağlantı `?focus=<id>` ile ilgili detay çekmecesini açar. `searchTerm` başlık/mesajın yanında `actorName` ve `referenceCode` alanlarında da arar. Bildirimler KVKK gereği TCKN/telefon/kart/sağlık detayı taşımaz.

## Fiyatlandırma — `/api/v1/pricing` (ADR-048)

Branş bazlı **baz prim tarifesi** yönetimi. Tarifeler **effective-dated ve değişmezdir**: fiyat değişikliği her zaman *yeni bir versiyon* oluşturur; güncelleme/silme ucu yoktur.

| Metot | Uç | Açıklama | Yetki |
|-------|----|----------|-------|
| `GET` | `/pricing/versions` | Yürürlükteki tarife + tüm fiyatlandırma geçmişi (en yeni önce; `isCurrent`/`isScheduled` ve önceki değer karşılaştırmasıyla) | **Yalnızca Admin** |
| `POST` | `/pricing/versions` | Yeni tarife versiyonu yayınlar (tüm branşlar zorunlu; `effectiveFrom` geçmiş olamaz) | **Yalnızca Admin** |

> **Yerleşik baz tarife (ADR-049):** `GET /pricing/versions` yanıtı, hiç özel tarife yayınlanmamış olsa bile listenin en altına sistemin **yerleşik baz tarifesini** `isBaseline: true` olan bir **v0** kaydı olarak ekler (`id` boş GUID, `effectiveFrom` anlamsız). Bu, admin ekranının "Varsayılan" yerine gerçek baz primleri göstermesini sağlar; değerler fiyatlama motoruyla tek kaynaktan (aynı sabitler) beslenir. Salt-okunur bir okuma zenginleştirmesidir — şema/migration değişmez.

> **Veri bütünlüğü garantisi:** Teklif oluşturulurken o an yürürlükteki versiyon teklifte **sabitlenir** (`Quote.PricingVersionId`). Teklif detayı, poliçe detayı ve PDF her zaman bu sabitlenen tarifeyle yeniden hesaplanır → tarife değişse bile **mevcut teklif ve poliçelerin primleri değişmez**; yalnızca değişiklikten sonra oluşturulan teklifler yeni tarifeyi kullanır (ADR-021 determinizmi korunur).

## Personel Yönetimi — `/api/v1/staff` (ADR-060)

Acente **Personel** hesaplarının yaşam döngüsü. **Tüm uçlar yalnızca `Admin`**'e açıktır (`Personel` ve `Customer` → `403`; anonim → `401`).

| Metot | Uç | Açıklama | Yetki |
|-------|----|----------|-------|
| `GET` | `/staff?page=&pageSize=&searchTerm=&isActive=` | Personel listesi (e-posta/ad araması + aktiflik filtresi; sayfalı). Yalnızca `Personel` rolündekiler listelenir | **Yalnızca Admin** |
| `GET` | `/staff/{id}` | Personel detayı. Hedef `Personel` değilse (Admin/Customer/yok) → `404` (varlık sızdırma yok — IDOR savunması) | **Yalnızca Admin** |
| `POST` | `/staff` | Yeni **Personel** hesabı oluşturur. Gövde: `{ email, fullName, password }` — **rol/isActive alanı YOKTUR**; rol sunucuda daima `Personel`'e sabittir (mass-assignment/escalation savunması). E-posta çakışması → `409` | **Yalnızca Admin** |
| `PUT` | `/staff/{id}` | Personelin görünen adını günceller (`{ fullName }`). E-posta/rol değişmez, aktiflik bu uçtan yönetilmez. Hedef `Personel` değilse → `404` | **Yalnızca Admin** |
| `PATCH` | `/staff/{id}/status` | Personeli aktif/pasif yapar (`{ isActive }`) → `204`. Pasifleştirmede kullanıcının **tüm refresh token'ları iptal edilir** (ADR-061). Hedef **yalnızca Personel** olabilir → hiçbir Admin pasifleştirilemez (son-Admin invariant'ı) | **Yalnızca Admin** |

> **Bilinçli olarak YOK olan uçlar:** Rol atama/değiştirme, Admin oluşturma, şifre sıfırlama (personel `POST /auth/forgot-password` kullanır) ve **silme (DELETE)**. Personel kendi şifresini mevcut `POST /auth/change-password` ile değiştirir; kendi kaydını Staff API üzerinden yönetemez.

> **Hesap aktifliği (ADR-061):** `AppUser.IsActive` hesap erişiminin temel kontrolüdür. **Pasif hesap** `POST /auth/login` ve `POST /auth/refresh-token` ile **erişim alamaz** (genel hata mesajı — aktiflik durumu sızdırılmaz). Access token stateless olduğundan pasifleştirmenin etkisi en geç **access token ömrü kadar (≤ 15 dk)** içinde tamamlanır; eldeki refresh token'lar ise anında iptal edilir.

## Sistem

| Metot | Uç | Açıklama | Yetki |
|-------|----|----------|-------|
| `GET` | `/health` | Veritabanı bağlantısını yoklayan JSON sağlık yanıtı | Anonim |
| `WS` | `/hubs/notifications` | Gerçek zamanlı bildirim hub'ı (SignalR; JWT `access_token` query — ADR-041). Sunucu→istemci `notification` olayı; Admin/Personel "staff" grubuna yayın | Kimliği doğrulanmış |
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
