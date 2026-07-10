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

## Durum

Proje şu anda **FAZ 0 — Temel & İskelet** aşamasını tamamlamıştır (Task 1–6 tamamlandı; kimlik doğrulama, yetkilendirme ve prodüksiyon standardında API çapraz kesit altyapısı hazır). Sonraki adım FAZ 1 iş modülleridir (Task 7 — Müşteri & Profil Modülü). Güncel görev listesi için bkz. [`docs/ai/TASKS.md`](docs/ai/TASKS.md).
