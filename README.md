# SigortaPro

Tek acenteli, B2C sigorta poliçe yönetim sistemi (MVP). Müşteriler self-servis teklif alır, karşılaştırır, satın alır, poliçe PDF'ini indirir, hasar bildirir ve yenileme tekliflerini onaylar; acente personeli dashboard, raporlama ve hasar karar süreçlerini yönetir.

## Stack

- **Backend:** ASP.NET Core Web API (.NET 8), EF Core (Code-First), SQL Server, MediatR (CQRS), JWT
- **Frontend:** React 18 + TypeScript + Vite 8, TanStack Query, Tailwind CSS (bkz. [`frontend/README.md`](frontend/README.md))
- **Mimari:** Clean Architecture (Domain → Application → Infrastructure/Persistence → WebAPI)

## Dokümantasyon

| Doküman | İçerik |
|---------|--------|
| [`ARCHITECTURE.md`](ARCHITECTURE.md) | Katman yapısı, domain modeli, modül detayları, test altyapısı |
| [`API.md`](API.md) | Tüm API uçlarının özeti (yetkiler, parametreler, enum sözleşmesi, durum kodları) |
| [`PRICING.md`](PRICING.md) | Fiyatlama motoru kuralları (baz primler, çarpanlar, risk skoru) |
| `docs/ai/DECISIONS.md` | Mimari karar kayıtları, ADR-001…034 (yerel geliştirme dokümanı — `.gitignore` ile repo dışında) |
| [`frontend/README.md`](frontend/README.md) | SPA kurulumu, klasör yapısı, frontend mimari notları |

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
├── frontend/                           ← React SPA (müşteri portalı + admin paneli)
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
- Node.js 20.19+ veya 22.12+ (frontend — Vite 8 gereksinimi)

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

### Frontend (React SPA)

```bash
cd frontend
npm install
cp .env.example .env    # varsayılan: VITE_API_BASE_URL=http://localhost:5153/api/v1
npm run dev             # http://localhost:5173 (backend CORS bu origin'e açık)
npm run build           # prodüksiyon derlemesi (tsc + vite → dist/)
```

Detaylı kurulum, klasör yapısı ve mimari notlar için bkz. [`frontend/README.md`](frontend/README.md).

> **Not:** Development connection string `appsettings.Development.json` içinde yerel SQL Server Express instance'ını (`.\SQLEXPRESS`) hedefler; kendi ortamınıza göre düzenleyebilir veya `SIGORTAPRO_DESIGN_CONNECTION` ortam değişkeniyle geçersiz kılabilirsiniz. `dotnet run` Development ortamında `Database.MigrateAsync()` + `DbSeeder` + `IdentitySeeder` çağırır; ilk çalıştırmada veritabanı, ürünler, örnek müşteri, roller ve seed kullanıcıları otomatik oluşur.
>
> JWT imzalama anahtarı Development'ta `appsettings.Development.json > JwtSettings:SecretKey` içinde bir **placeholder**'dır; yerel çalıştırmadan önce en az 32 karakterlik bir değerle değiştirin (ör. `dotnet user-secrets`). **Üretimde** `appsettings.json`'daki `SecretKey` boştur ve deploy sırasında ortam değişkeni / user-secrets ile sağlanmalıdır (boşsa uygulama başlangıçta hata verir — fail-fast).

## Ekran Görüntüleri

> Yer tutucular — görüntüler `docs/screenshots/` altına eklendiğinde otomatik görünür.

| Müşteri Portalı | Admin Paneli |
|-----------------|--------------|
| ![Teklif sihirbazı — anlık prim + paket karşılaştırma](docs/screenshots/quote-wizard.png) | ![Admin dashboard — KPI kartları + grafikler](docs/screenshots/admin-dashboard.png) |
| ![Ödeme sayfası — taksit seçimi + test kartları](docs/screenshots/purchase.png) | ![Hasar yönetimi — karar akışı + zaman çizelgesi](docs/screenshots/admin-claims.png) |
| ![Poliçelerim — durum sekmeleri + PDF indirme](docs/screenshots/policies.png) | ![Müşteri yönetimi — arama + detay çekmecesi](docs/screenshots/admin-customers.png) |

## Testler (Task 21)

```bash
dotnet test                 # tüm paket: 263 test (birim + entegrasyon)
```

- **Birim testler** — `tests/SigortaPro.{Domain,Application,Infrastructure}.Tests`: fiyatlama motoru kural senaryoları, domain durum makineleri (Quote/Policy/Claim/Payment/Renewal), handler/validator/pipeline testleri (xUnit + FluentAssertions + NSubstitute).
- **Entegrasyon testleri** — `tests/SigortaPro.WebAPI.Tests/Integration`: `WebApplicationFactory` + **SQLite in-memory** ile gerçek HTTP pipeline (middleware → JWT → MediatR → EF Core → Identity) test edilir; **hiçbir dış bağımlılık gerekmez** (SQL Server/Docker kurulumu olmadan çalışır). Kapsam: auth akışı (register → login → refresh rotasyonu + negatifler) ve teklif→satın alma akışı (teklif → onay → mock POS → aktif poliçe; 402/409/403 senaryoları). Tasarım kararları için bkz. ADR-034.

> **Tüm API uçlarının derli toplu özeti** (müşteri/teklif uçları dahil, yetki ve parametreleriyle) için bkz. [`API.md`](API.md); canlı şema dokümantasyonu için Development'ta Swagger (`/swagger`). Aşağıdaki bölümler öne çıkan akışların bağlamlı özetidir.

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

## Ödeme & Poliçe Endpoint'leri (Task 10, 11, 18)

| Metot | Endpoint | Açıklama | Yetki |
|-------|----------|----------|-------|
| `POST` | `/api/v1/payments` | Onaylanmış teklifi mock POS ile satın al → ödeme + aktif poliçe (başarısızsa `402`) | `Customer` |
| `GET` | `/api/v1/payments` | Ödeme geçmişi (sayfalı; maskeli kart) | `Customer` |
| `GET` | `/api/v1/payments/installment-options?quoteId=` | Onaylanmış teklifin taksit seçenekleri | `Customer` |
| `GET` | `/api/v1/policies` | **Poliçelerim**: müşterinin poliçeleri (sayfalı + durum filtresi) — Task 18 | `Customer` |
| `GET` | `/api/v1/policies/{id}` | Poliçe detayı (teminat tablosu ile) — Task 18 | Sahip müşteri / personel |
| `GET` | `/api/v1/policies/{id}/document` | Poliçe sertifikası PDF'i (ilk erişimde üretilir) | Sahip müşteri / personel |

> `GET /policies` ve `GET /policies/{id}` **salt okunur, additive** uçlardır (Task 18, ADR-031); mevcut hiçbir uç/DTO/şema değişmemiştir (migration yok).

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

Acente personeli (Admin/Personel) için admin panelinin veri kaynağı. Tüm uçlar **salt okunur** ve `[Authorize(Roles = Staff)]` ile korunur; metrikler SQL tarafı agregasyonla (projection + `AsNoTracking`) üretilir (bkz. ADR-026 — `docs/ai/DECISIONS.md`, yerel).

| Metot | Endpoint | Açıklama | Yetki |
|-------|----------|----------|-------|
| `GET` | `/api/v1/dashboard/summary` | Özet metrikler: prim üretimi, aktif poliçe, bekleyen teklif/hasar, yenileme oranı, hasar/prim oranı, aylık satış trendi (son 12 ay), branş dağılımı | `Staff` |
| `GET` | `/api/v1/dashboard/reports/policies?from=&to=&page=&pageSize=` | Tarih aralıklı poliçe raporu (başlangıç tarihine göre, sayfalı) | `Staff` |
| `GET` | `/api/v1/dashboard/reports/payments?from=&to=&page=&pageSize=` | Tarih aralıklı ödeme raporu (işlem tarihine göre, sayfalı) | `Staff` |
| `GET` | `/api/v1/dashboard/reports/riskiest-customers?top=` | En riskli müşteri segmentleri (hasar sayısına göre ilk N) | `Staff` |

> **Not:** Oranlar 0–1 aralığında ondalık döner (frontend biçimlendirir): yenileme oranı = onaylanan/sunulan yenileme; hasar/prim oranı = ödenen hasar tutarı/üretilen prim. Rapor uçlarında `from`/`to` **dahil** (inclusive) tarih aralığıdır ve `to`, `from`'dan önce olamaz (`400`).

## Durum

**MVP tamamlandı** — tüm fazlar (FAZ 0–3, Task 1–22) teslim edilmiştir:

- **FAZ 0 — Temel & İskelet (Task 1–6):** Clean Architecture solution'ı, domain modeli, CQRS omurgası, EF Core + SQL Server, JWT + Identity kimlik doğrulama, cross-cutting API altyapısı (RFC 7807 hata zarfı, Serilog, CORS, Swagger, rate limiting, güvenlik header'ları, health check).
- **FAZ 1 — Çekirdek İş Modülleri (Task 7–14):** Müşteri & profil, kural tabanlı mock fiyatlama motoru, teklif (durum makinesi + paket karşılaştırma), mock POS ödeme + poliçeleştirme, QuestPDF poliçe sertifikası, iki taraflı hasar süreci, otomatik poliçe yenileme (arkaplan servisi + hasar geçmişi çarpanı), admin dashboard & raporlama API'si.
- **FAZ 2 — Frontend (Task 15–20):** React SPA — oturum akışı, profil + risk objesi yönetimi, çok adımlı teklif sihirbazı (anlık prim + risk skoru + paket karşılaştırma), ödeme sayfası + başarı ekranı, Poliçelerim (PDF indirme), hasar bildirim/takip, yenileme onayı ve tam admin paneli (dashboard grafikleri + yönetim ekranları + hasar karar akışı). Seed kullanıcılarıyla giriş yapılabilir (yukarıdaki tablo).
- **FAZ 3 — Kalite & Teslim (Task 21–22):** 263 testlik yeşil paket (birim + WebApplicationFactory/SQLite entegrasyon testleri) ve dokümantasyon finali ([`ARCHITECTURE.md`](ARCHITECTURE.md), [`API.md`](API.md), bu README).

Bilinçli MVP sınırları (gerçek POS/e-posta/foto depolama entegrasyonları mock'tur) ve gelecek faz adayları için bkz. `docs/ai/PROJECT_CONTEXT.md` §9 (yerel) ve [`ARCHITECTURE.md`](ARCHITECTURE.md).
