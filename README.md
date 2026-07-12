# SigortaPro

Tek acenteli, B2C sigorta poliçe yönetim sistemi (MVP).

## Stack

- **Backend:** ASP.NET Core Web API (.NET 8), EF Core (Code-First), SQL Server, MediatR (CQRS), JWT
- **Frontend:** React + TypeScript + Vite (bkz. `frontend/`, FAZ 2'de eklenecek)
- **Mimari:** Clean Architecture (Domain → Application → Infrastructure/Persistence → WebAPI)

Detaylı mimari için bkz. [`ARCHITECTURE.md`](ARCHITECTURE.md).

## Proje Yapısı

```
SigortaPro/
├── src/
│   ├── Core/
│   │   ├── SigortaPro.Domain/          ← Entity, enum, domain event, sabitler (sıfır bağımlılık)
│   │   └── SigortaPro.Application/     ← CQRS, DTO, arayüzler, validasyon
│   ├── Infrastructure/
│   │   ├── SigortaPro.Persistence/     ← EF Core, DbContext, repository, migration
│   │   └── SigortaPro.Infrastructure/  ← PDF, e-posta mock, ödeme mock, JWT token servisi
│   └── Presentation/
│       └── SigortaPro.WebAPI/          ← Controller, middleware, DI kompozisyonu
├── tests/
│   ├── SigortaPro.Domain.Tests/
│   ├── SigortaPro.Application.Tests/
│   ├── SigortaPro.Infrastructure.Tests/
│   └── SigortaPro.WebAPI.Tests/
├── docs/ai/                            ← AI asistan yönerge dokümanları
├── SigortaPro.sln
├── Directory.Build.props
└── .editorconfig
```

## Gereksinimler

- .NET 8 SDK
- SQL Server Express (geliştirme ortamı — bkz. `appsettings.Development.json`)
- Node.js 18+ — frontend için, FAZ 2'de eklenecek

## Kurulum ve Çalıştırma

```bash
# Solution'ı derle
dotnet build

# Testleri çalıştır
dotnet test

# Veritabanı migration'larını uygula (Persistence projesinden)
cd src/Infrastructure/SigortaPro.Persistence
dotnet ef database update
cd ../../..

# API'yi çalıştır (Development ortamında başlangıçta migrate + seed otomatik çalışır)
dotnet run --project src/Presentation/SigortaPro.WebAPI
```

> **Not:** Development connection string `appsettings.Development.json` içinde yerel SQL Server Express instance'ını (`.\SQLEXPRESS`) hedefler; kendi ortamınıza göre düzenleyebilir veya `SIGORTAPRO_DESIGN_CONNECTION` ortam değişkeniyle geçersiz kılabilirsiniz. `dotnet run` Development ortamında `Database.MigrateAsync()` + `DbSeeder` + `IdentitySeeder` çağırır; ilk çalıştırmada veritabanı, ürünler, örnek müşteri, roller ve seed kullanıcıları otomatik oluşur.
>
> JWT imzalama anahtarı Development'ta `appsettings.Development.json > JwtSettings:SecretKey` içinde bir **placeholder**'dır; yerel çalıştırmadan önce en az 32 karakterlik bir değerle değiştirin (ör. `dotnet user-secrets`). **Üretimde** `appsettings.json`'daki `SecretKey` boştur ve deploy sırasında ortam değişkeni / user-secrets ile sağlanmalıdır (boşsa uygulama başlangıçta hata verir — fail-fast).

## Kimlik Doğrulama Endpoint'leri (Task 5)

| Metot | Endpoint | Açıklama |
|-------|----------|----------|
| `POST` | `/api/v1/auth/register` | Müşteri kaydı (Customer profili ile birlikte) — access + refresh token döner |
| `POST` | `/api/v1/auth/login` | Giriş — access + refresh token döner |
| `POST` | `/api/v1/auth/refresh-token` | Refresh token ile yeni access + refresh token (rotasyonlu) |

Access token 15 dakika, refresh token 7 gün geçerlidir; token yenilendiğinde eski refresh token iptal edilir. Kimlik doğrulama uçları rate limit ile korunur (IP başına dakikada 10 istek; aşımda `429`).

## API Altyapısı (Task 6)

- **Swagger/OpenAPI:** Development ortamında `GET /swagger` (JWT "Authorize" desteğiyle). API çalışırken ör. `http://localhost:5153/swagger`.
- **Health check:** `GET /health` — veritabanı bağlantısını kontrol eden JSON yanıt (`{"status":"Healthy",...}`).
- **Hata formatı:** Tüm hatalar RFC 7807 `ProblemDetails` (`application/problem+json`) olarak döner; her yanıtta `traceId` ve `correlationId` bulunur.
- **Loglama:** Serilog ile console + günlük rolling dosya (`logs/sigortapro-.log`, 7 gün saklanır — `.gitignore` ile hariç). Her istek `X-Correlation-ID` header'ıyla ilişkilendirilir (gelen header korunur, yoksa üretilir).
- **CORS:** `appsettings.json > Cors:AllowedOrigins` (varsayılan Vite dev sunucusu `http://localhost:5173`).
- **Güvenlik header'ları:** `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`; HSTS yalnızca üretimde.

### Seed Test Kullanıcıları (yalnızca geliştirme)

| Rol | E-posta | Şifre |
|-----|---------|-------|
| Admin | `admin@sigortapro.com` | `Admin!2345` |
| Customer | `musteri@sigortapro.com` | `Musteri!2345` |

> ⚠️ Bu kimlik bilgileri yalnızca geliştirme seed'i içindir; üretimde kullanılmamalıdır.

### Mock Sanal POS Test Kartları (yalnızca geliştirme)

Ödeme akışı (`POST /api/v1/payments`) gerçek POS yerine `MockVirtualPosService` kullanır (ADR-007). Kart numarası Luhn ile doğrulanır; aşağıdaki senaryo kartları belirli sonuçlar üretir. Kart numarası **asla** tam saklanmaz (yalnızca son 4 hane).

| Kart Numarası | Sonuç |
|---------------|-------|
| `4111 1111 1111 1111` (veya Luhn-geçerli diğer kartlar) | Başarılı ödeme → poliçe oluşur |
| `4000 0000 0000 0002` | Başarısız — "Yetersiz bakiye." (402) |
| `4000 0000 0000 0069` | Başarısız — "3D Secure doğrulaması başarısız." (402) |
| Luhn-geçersiz numara | Başarısız — "Geçersiz kart numarası." (402) |

> İzin verilen taksit sayıları: **1, 3, 6, 9, 12** (faizsiz mock; toplam tutar sabit).

## Hasar (Claim) Endpoint'leri (Task 12)

İki taraflı hasar süreci: müşteri bildirir, acente personeli (Admin/Personel) inceleyip karara bağlar ve öder. Hasar durum makinesi: `Submitted → UnderReview → Approved/Rejected → Paid`.

| Metot | Endpoint | Açıklama | Yetki |
|-------|----------|----------|-------|
| `POST` | `/api/v1/claims` | Hasar bildirimi (aktif poliçe + dönem içi olay + tahmini tutar + mock foto) | `Customer` |
| `GET` | `/api/v1/claims` | Hasar listesi (müşteri kendi / personel tümü; durum/poliçe filtresi + sayfalama) | Kimliği doğrulanmış |
| `GET` | `/api/v1/claims/{id}` | Hasar detayı (sahiplik kontrollü) | Sahip müşteri / personel |
| `POST` | `/api/v1/claims/{id}/start-review` | İncelemeye al (Submitted → UnderReview) | `Admin`, `Personel` |
| `POST` | `/api/v1/claims/{id}/approve` | Onayla (→ Approved; onay tutarı + değerlendirme notu) | `Admin`, `Personel` |
| `POST` | `/api/v1/claims/{id}/reject` | Reddet (→ Rejected; gerekçe) | `Admin`, `Personel` |
| `POST` | `/api/v1/claims/{id}/pay` | Ödeme yap (Approved → Paid) | `Admin`, `Personel` |

> **Not:** Hasar yalnızca **aktif** poliçeye ve poliçe **dönemi içindeki** (gelecekte olmayan) bir olaya açılabilir; aksi halde `409` döner. **Foto yükleme mock'tur:** yüklenen dosya adları doğrulanır (`.jpg/.jpeg/.png/.pdf`, en fazla 10) ancak saklanmaz — gerçek hasar fotoğraf depolama MVP kapsamı dışıdır. Onaylanan/ödenen hasarların sayısı, ileride yenileme fiyatlamasına (Task 13) beslenmek üzere repository seviyesinde erişilebilir.

## Poliçe Yenileme (Renewal) Endpoint'leri (Task 13)

Bir arkaplan servisi (`PolicyLifecycleBackgroundService`) uygulama açılışında ve periyodik olarak (6 saat) çalışır: süresi geçen teklif/poliçeleri `Expired`'a çeker ve bitişine **≤30 gün** kalan poliçeler için otomatik yenileme teklifi üretir (güncel fiyatlama + **hasar geçmişi çarpanı** — bkz. [`PRICING.md`](PRICING.md) §6.1). Üretilen teklif için müşteriye mock bildirim (log) gönderilir.

| Metot | Endpoint | Açıklama | Yetki |
|-------|----------|----------|-------|
| `GET` | `/api/v1/renewals` | Müşterinin yenileme teklifleri (sayfalı) | `Customer` |
| `POST` | `/api/v1/renewals/{id}/accept` | Yenilemeyi onayla → yeni teklif `Approved` olur (ödemeye hazır) | `Customer` |

> **Not:** Yenileme onaylandıktan sonra ödeme ve yeni dönem poliçesi oluşturma **mevcut** ödeme akışıyla (`POST /api/v1/payments`) yürür — yenileme için ayrı bir ödeme ucu yoktur. Hasarlı müşterinin yenileme primi, önceki dönem hasar geçmişine göre artar (her `Approved`/`Paid` hasar +%20, tavan +%60).

## Admin Dashboard & Raporlama Endpoint'leri (Task 14)

Acente personeli (Admin/Personel) için admin panelinin veri kaynağı. Tüm uçlar **salt okunur** ve `[Authorize(Roles = Staff)]` ile korunur; metrikler SQL tarafı agregasyonla (projection + `AsNoTracking`) üretilir (bkz. [`docs/ai/DECISIONS.md`](docs/ai/DECISIONS.md) ADR-026).

| Metot | Endpoint | Açıklama | Yetki |
|-------|----------|----------|-------|
| `GET` | `/api/v1/dashboard/summary` | Özet metrikler: prim üretimi, aktif poliçe, bekleyen teklif/hasar, yenileme oranı, hasar/prim oranı, aylık satış trendi (son 12 ay), branş dağılımı | `Staff` |
| `GET` | `/api/v1/dashboard/reports/policies?from=&to=&page=&pageSize=` | Tarih aralıklı poliçe raporu (başlangıç tarihine göre, sayfalı) | `Staff` |
| `GET` | `/api/v1/dashboard/reports/payments?from=&to=&page=&pageSize=` | Tarih aralıklı ödeme raporu (işlem tarihine göre, sayfalı) | `Staff` |
| `GET` | `/api/v1/dashboard/reports/riskiest-customers?top=` | En riskli müşteri segmentleri (hasar sayısına göre ilk N) | `Staff` |

> **Not:** Oranlar 0–1 aralığında ondalık döner (frontend biçimlendirir): yenileme oranı = onaylanan/sunulan yenileme; hasar/prim oranı = ödenen hasar tutarı/üretilen prim. Rapor uçlarında `from`/`to` **dahil** (inclusive) tarih aralığıdır ve `to`, `from`'dan önce olamaz (`400`).

## Durum

Proje şu anda **FAZ 0 — Temel & İskelet** aşamasını ve **FAZ 1 — Çekirdek İş Modülleri**'ni (Task 7–14: Müşteri & Profil, Fiyatlama Motoru, Teklif, Ödeme & Poliçeleştirme, PDF Poliçe Dökümanı, Hasar Modülü, Poliçe Yenileme & Arkaplan İşleri, Admin Dashboard & Raporlama API'si) tamamlamıştır. Teklif → ödeme → poliçe → PDF, hasar süreci, otomatik poliçe yenileme ve admin dashboard/raporlama API'si uçtan uca çalışır durumdadır. Backend MVP tamamlanmıştır; sonraki adım FAZ 2 — Task 15 — React Proje Kurulumu & Tasarım Sistemi'dir. Güncel görev listesi için bkz. [`docs/ai/TASKS.md`](docs/ai/TASKS.md).
