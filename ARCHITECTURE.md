# SigortaPro — Mimari Doküman

## Katman Yapısı (Clean Architecture)

```
┌──────────────────────────────────────────────────────────────┐
│                    Presentation (WebAPI)                      │
│  Controllers, Middleware, Filters, DI Composition Root       │
├──────────────────────────────────────────────────────────────┤
│                    Infrastructure                             │
│  EF Core, SQL Server, JWT, PDF, Mock Services, Background    │
│  ┌─────────────────────┐  ┌──────────────────────────────┐  │
│  │   Persistence       │  │   Infrastructure              │  │
│  │   (DbContext, Repo) │  │   (Token, Pricing, POS, PDF) │  │
│  └─────────────────────┘  └──────────────────────────────┘  │
├──────────────────────────────────────────────────────────────┤
│                    Application (Core)                         │
│  CQRS Handlers, DTOs, Validators, Interfaces, Behaviors      │
├──────────────────────────────────────────────────────────────┤
│                    Domain (Core)                              │
│  Entities, Enums, Value Objects, Domain Events, Constants    │
└──────────────────────────────────────────────────────────────┘
```

**Bağımlılık yönü:** dıştan içe. `Domain` hiçbir şeye bağımlı değildir (sıfır NuGet paketi); `Application` yalnızca `Domain`'e bağımlıdır; `Persistence` ve `Infrastructure` ikisi de `Domain` + `Application`'a bağımlıdır ve birbirlerini referans almazlar; `WebAPI` composition root olarak tüm katmanları bir araya getirir.

## Solution Yapısı

| Proje | Katman | Bağımlılıkları |
|-------|--------|-----------------|
| `SigortaPro.Domain` | Core | — (sıfır bağımlılık) |
| `SigortaPro.Application` | Core | `Domain` |
| `SigortaPro.Persistence` | Infrastructure | `Domain`, `Application` |
| `SigortaPro.Infrastructure` | Infrastructure | `Domain`, `Application` |
| `SigortaPro.WebAPI` | Presentation | `Domain`, `Application`, `Persistence`, `Infrastructure` |

Test projeleri (`tests/`) ilgili katmanla bire bir eşleşir ve yalnızca test ettiği katmana referans verir.

## Build Altyapısı

- **`Directory.Build.props`**: Tüm projeler için ortak derleyici ayarları merkezi olarak tanımlanır — `TargetFramework` (`net8.0`), `Nullable` (enable), `ImplicitUsings` (enable), `LangVersion` (12.0), .NET analyzer'ları (`EnableNETAnalyzers`, `AnalysisLevel=latest-recommended`).
- **`.editorconfig`**: Naming convention'ları (interface `I` prefiksi, private field `_camelCase`, async metot `Async` soneki) analyzer kuralı olarak derleme zamanında uygular; genel formatlama kuralları (girinti, satır sonu, dosya sonu boş satır) tanımlanır.
- **`.gitignore`**: `bin/`, `obj/`, IDE dosyaları, test sonuçları, gizli konfigürasyon dosyaları ve (ileride) `frontend/node_modules` hariç tutulur.

## Katman Detayları

### Domain (`SigortaPro.Domain`)
Entity, enum, domain event ve iş sabitlerini barındırır. Üçüncü parti NuGet paketi (MediatR dahil) içermez; yalnızca .NET BCL tipleri kullanılır. Klasör iskeleti: `Common/`, `Entities/`, `Enums/`, `Events/`, `Constants/`.

Detaylı domain modeli için bkz. [Domain Modeli](#domain-modeli) bölümü.

### Application (`SigortaPro.Application`)
CQRS handler'ları, DTO'lar, FluentValidation validator'ları, repository/servis arayüzleri ve MediatR pipeline behavior'larını barındırır. Klasör iskeleti: `Common/{Behaviors,Exceptions,Interfaces,Models}/`, `Features/{Modül}/{Commands,Queries,DTOs}/`.

### Persistence (`SigortaPro.Persistence`)
EF Core `AppDbContext`, entity konfigürasyonları, generic repository implementasyonları, migration'lar ve seed verisi. Klasör iskeleti: `Context/`, `Configurations/`, `Repositories/`, `Migrations/`, `Seed/`, `Interceptors/`.

### Infrastructure (`SigortaPro.Infrastructure`)
Domain dışı teknik servisler: JWT token üretimi, mock fiyatlama motoru, mock sanal POS, QuestPDF ile poliçe dokümanı üretimi, mock bildirim servisi, arkaplan işleri. Klasör iskeleti: `Services/`, `BackgroundJobs/`.

### WebAPI (`SigortaPro.WebAPI`)
Controller'lar, middleware, filter'lar ve DI composition root. Klasör iskeleti: `Controllers/v1/`, `Middleware/`, `Filters/`, `Extensions/`.

## Domain Modeli

### Yapı Taşları (`Common/`)

| Tip | Açıklama |
|-----|----------|
| `BaseEntity` | Tüm entity'lerin türediği taban sınıf: `Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted`, `CreatedBy`, `UpdatedBy`. |
| `IAggregateRoot` | Aggregate root işaretleyici — yalnızca bu arayüzü uygulayan entity'ler repository üzerinden doğrudan erişilir (`Coverage` ve `PolicyDocument` birer child entity'dir, aggregate root değildir). |
| `Address` | Değişmez (immutable) value object — `City`, `District`, `Neighborhood`, `PostalCode`. `Customer` ve `Property` tarafından kullanılır. |
| `DomainException` | Entity'lerin durum makinesi/iş kuralı ihlallerinde fırlattığı domain-özel exception. Application katmanı bunu yakalayıp `BusinessRuleException`'a çevirir (bkz. ADR-013). |

### Enum'lar (`Enums/`)

`InsuranceBranch` (Kasko, Trafik, Konut, Dask, Saglik) · `UserRole` (Admin, Personel, Customer — rol adları Task 5'te Identity rollerine seed edilir) · `QuoteStatus` (Draft, Priced, Approved, Purchased, Expired, Rejected) · `PolicyStatus` (Active, Expired, Cancelled) · `ClaimStatus` (Submitted, UnderReview, Approved, Rejected, Paid) · `PaymentStatus` (Pending, Successful, Failed)

### İş Sabitleri (`Constants/BusinessConstants`)

`TcknLength` (11) · `MaxQuoteValidityDays` (7) · `RenewalNoticeWindowDays` (30) · `PolicyNumberPrefix` ("POL") · `MaskedCardVisibleDigits` (4)

### Durum Makineleri

```
Quote:  Draft → Priced → Approved → Purchased
                       ↘ Expired
                       ↘ Rejected

Policy: Active → Expired
        Active → Cancelled

Claim:  Submitted → UnderReview → Approved → Paid
                                ↘ Rejected
```

Geçiş kuralları ilgili entity'nin (`Quote`, `Policy`, `Claim`, `Renewal`, `Payment`) genel metotlarında uygulanır (`MarkAsPriced`, `Approve`, `Purchase`, `Reject`, `Expire`, `Cancel`, `ExpireIfPastEndDate`, `StartReview`, `MarkPaid`, `Accept`, `MarkSuccessful`, `MarkFailed`); geçersiz bir geçiş `DomainException` fırlatır. Bu tasarım, entity'lerin anemic olmasını engeller (bkz. `CLAUDE.md` §10 "Anemic domain model hariç").

### Entity İlişki Diyagramı

> Not: Kimlik (Identity) kullanıcısı Domain modelinin parçası değildir; `Customer.AppUserId`, Persistence katmanındaki `AppUser : IdentityUser<Guid>` kaydına işaret eden düz bir Guid'dir (bkz. ADR-014).

```mermaid
erDiagram
    Customer ||--o{ Vehicle : sahip
    Customer ||--o{ Property : sahip
    Customer ||--o{ Quote : talep_eder
    Customer ||--o{ Policy : sahip
    Customer ||--o{ Claim : bildirir
    Customer ||--o{ Payment : oder
    InsuranceProduct ||--o{ Coverage : icerir
    InsuranceProduct ||--o{ Quote : "fiyatlanir_icin"
    Vehicle |o--o{ Quote : "risk objesi (Kasko/Trafik)"
    Property |o--o{ Quote : "risk objesi (Konut/DASK)"
    Quote ||--o| Policy : "satin_alinir"
    Quote ||--o{ Payment : "odenir"
    Quote |o--o| Renewal : "yenileme_teklifi"
    Policy ||--o| PolicyDocument : uretir
    Policy ||--o{ Claim : kapsar
    Policy ||--o{ Renewal : "suresi_dolan"

    Customer {
        Guid Id PK
        Guid AppUserId "Identity kullanicisina referans (Persistence)"
        string FirstName
        string LastName
        string Tckn
        DateTime BirthDate
        string PhoneNumber
    }
    InsuranceProduct {
        Guid Id PK
        string Name
        InsuranceBranch Branch
        bool IsActive
    }
    Coverage {
        Guid Id PK
        Guid InsuranceProductId FK
        string Name
        decimal DefaultLimit
    }
    Vehicle {
        Guid Id PK
        Guid CustomerId FK
        string PlateNumber
        string Brand
        string Model
        int ManufactureYear
        int EnginePowerHp
    }
    Property {
        Guid Id PK
        Guid CustomerId FK
        int BuildingAge
        int SquareMeters
        int EarthquakeZone
    }
    Quote {
        Guid Id PK
        Guid CustomerId FK
        Guid InsuranceProductId FK
        Guid VehicleId FK
        Guid PropertyId FK
        InsuranceBranch Branch
        QuoteStatus Status
        decimal TotalPremium
        DateTime ValidUntil
    }
    Policy {
        Guid Id PK
        string PolicyNumber
        Guid CustomerId FK
        Guid QuoteId FK
        DateTime StartDate
        DateTime EndDate
        decimal TotalPremium
        PolicyStatus Status
    }
    PolicyDocument {
        Guid Id PK
        Guid PolicyId FK
        string FilePath
        DateTime GeneratedAt
    }
    Payment {
        Guid Id PK
        Guid CustomerId FK
        Guid QuoteId FK
        decimal Amount
        int InstallmentCount
        string MaskedCardNumber
        PaymentStatus Status
    }
    Claim {
        Guid Id PK
        Guid PolicyId FK
        Guid CustomerId FK
        DateTime IncidentDate
        decimal EstimatedAmount
        decimal ApprovedAmount
        ClaimStatus Status
    }
    Renewal {
        Guid Id PK
        Guid PolicyId FK
        Guid NewQuoteId FK
        DateTime OfferedAt
        bool IsAccepted
    }
```

### Kimlik (Identity) Sınırı

Kullanıcı/kimlik kavramı Domain modelinin **dışındadır** (ADR-014):

- `AppUser : IdentityUser<Guid>` sınıfı Task 5'te **Persistence katmanında** (`Persistence/Identity/`) tanımlanır; `AppDbContext`, `IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>`'ten türer.
- Domain'deki `Customer`, kullanıcıya yalnızca `AppUserId` (Guid) değeriyle bağlanır; navigation property yoktur.
- Application katmanı kullanıcı işlemlerine `IIdentityService` soyutlaması üzerinden erişir (arayüz Application'da, implementasyon Persistence'ta — `UserManager<AppUser>` kullanır).
- `ITokenService` (JWT üretimi) primitif değerlerle çalışır ve Infrastructure'da kalır; Infrastructure ↛ Persistence kuralı korunur.
- Refresh token kaydı Identity'nin yanında Persistence'ta tutulur.
- `UserRole` enum'u (Domain) rol adlarının tek kaynağıdır; Task 5'te Identity rollerine seed edilir.

Bu desen, .NET Clean Architecture ekosisteminin fiilî standardıdır (Jason Taylor CleanArchitecture şablonu, Microsoft eShop). Gerekçe ve alternatifler için bkz. ADR-014.

## Application Katmanı — CQRS Omurgası

`SigortaPro.Application`, MediatR (12.x — ADR-015) üzerine kurulu CQRS altyapısını barındırır. Klasör: `Common/{Behaviors,Exceptions,Interfaces,Models}/`.

### CQRS Marker'ları ve Handler Arayüzleri

`ICommand` / `ICommand<TResponse>` / `IQuery<TResponse>` (hepsi `MediatR.IRequest`'ten türer) ve bunlara karşılık gelen `ICommandHandler<TCommand>` / `ICommandHandler<TCommand, TResponse>` / `IQueryHandler<TQuery, TResponse>` arayüzleri, `IRequestHandler`'ı sarmalayarak Command/Query ayrımını tip sisteminde görünür kılar (ARCHITECTURE_RULES.md §3).

### Pipeline Behavior Zinciri

Kayıt sırası (`ApplicationServiceRegistration.AddApplicationServices`), ARCHITECTURE_RULES.md §3.5'te tanımlanan sırayla birebir eşleşir:

```
Request → ValidationBehavior → LoggingBehavior → PerformanceBehavior → UnhandledExceptionBehavior → Handler
```

- **`ValidationBehavior`** — `IValidator<TRequest>` kayıtlarını çalıştırır; hata varsa `Application.Common.Exceptions.ValidationException` fırlatır.
- **`LoggingBehavior`** — İstek başlangıç/bitişini structured log olarak yazar.
- **`PerformanceBehavior`** — 500ms üzeri süren handler'ları warning seviyesinde loglar.
- **`UnhandledExceptionBehavior`** — Domain'in `DomainException`'ını yakalayıp `BusinessRuleException`'a (409) çevirir (ADR-013); zaten bilinen `SigortaProException` alt tiplerini olduğu gibi yükseltir; beklenmeyen diğer hataları loglayıp yeniden fırlatır (middleware'in 500'e çevirmesi için — Task 6).

### Exception Hiyerarşisi

`SigortaProException` (abstract) → `NotFoundException` (404) · `ValidationException` (400, `IDictionary<string, string[]> Errors`) · `BusinessRuleException` (409) · `ForbiddenAccessException` (403) · `PaymentFailedException` (402, ödeme reddi — Task 10, ADR-022). Taban tipe düşen diğer türler 400'e eşlenir.

### Result / PagedResult / PaginationParams

`Result` / `Result<T>` — `IsSuccess`/`IsFailure`/`Errors` ve (generic sürümde) `Value` taşıyan, statik `Success()`/`Failure()` factory'li bir sonuç sarmalayıcısı (CA1000 bilinçli olarak suppress edilmiştir — bkz. kod içi gerekçe). `PagedResult<T>` — `Items`/`Page`/`PageSize`/`TotalCount`/`TotalPages`. `PaginationParams` — `PageSize` varsayılan 20, üst sınır 100 (DEVELOPMENT_RULES.md §6). Bu tipler, hangi command/query'nin bunları kullanacağına ilgili feature task'ında (Task 7+) karar verilecek; Task 3 yalnızca omurgayı kurar.

### Servis Arayüzleri

`ICurrentUserService` (impl. WebAPI, HttpContext tabanlı — Task 6), `IDateTimeProvider` (impl. Infrastructure — Task 6), `IReadRepository<T>` / `IWriteRepository<T>` / `IUnitOfWork` (impl. Persistence — Task 4) burada tanımlanır; tam liste için ARCHITECTURE_RULES.md §6.1.

## Persistence Katmanı — EF Core + SQL Server

`SigortaPro.Persistence`, `AppDbContext` etrafında kurulu EF Core Code-First altyapısını barındırır. Klasör: `Context/`, `Configurations/{Common/}`, `Interceptors/`, `Repositories/`, `Seed/`, `Migrations/`.

### AppDbContext ve Konfigürasyonlar

`AppDbContext`, yalnızca 9 aggregate root'u (`Customer`, `InsuranceProduct`, `Vehicle`, `Property`, `Quote`, `Policy`, `Payment`, `Claim`, `Renewal`) `DbSet<T>` olarak dışa açar; `Coverage` ve `PolicyDocument` kendi `IEntityTypeConfiguration`'larıyla modele dahildir ancak yalnızca sahibi aggregate root'un navigation'ı üzerinden erişilir (ARCHITECTURE_RULES.md §4.2). Tüm konfigürasyonlar ortak bir `BaseEntityConfiguration<T>` taban sınıfından türer; bu taban sınıf `Id`, audit alanları (`CreatedAt`/`CreatedBy`/`UpdatedBy` max 256 karakter) ve soft-delete `HasQueryFilter`'ı merkezi olarak uygular (ADR-010), her entity konfigürasyonu yalnızca kendine özgü kuralları (`ToTable`, `HasMaxLength`, `decimal(18,2)`, index, ilişki) ekler.

Öne çıkan kurallar:
- Para alanları (`TotalPremium`, `Amount`, `DefaultLimit`, `EstimatedAmount`, `ApprovedAmount`) `decimal(18,2)`.
- Benzersiz index'ler: `UQ_Customers_TCKN`, `UQ_Customers_AppUserId`, `UQ_Policies_PolicyNumber`, `UQ_PolicyDocuments_PolicyId`.
- `Address` value object'i (`Customer`, `Property`) `OwnsOne` ile aynı tabloya gömülü kolon (`City`/`District`/`Neighborhood`/`PostalCode`) olarak eşlenir.
- `Customer`'dan başlayan tüm ilişkiler (`Vehicle`, `Property`, `Quote`, `Policy`, `Claim`) `DeleteBehavior.Restrict` kullanır — SQL Server'ın çoklu cascade path kısıtlamasını aşmak için; `Policy → PolicyDocument` tek yönlü olduğundan `Cascade` güvenlidir.

### Repository ve UnitOfWork

`GenericRepository<T>`, Task 3'te tanımlanan `IReadRepository<T>` ve `IWriteRepository<T>`'yi tek sınıfta implement eder (`AsNoTracking` salt-okunur sorgularda, `GetPagedAsync` ile sayfalama). `Delete(T entity)` fiziksel silme yapmaz; `IsDeleted = true` işaretleyip `Update` çağırır (ADR-010). `UnitOfWork`, `AppDbContext.SaveChangesAsync`'i sarmalar.

### Audit Interceptor ve Soft Delete

`AuditableEntityInterceptor` (`SaveChangesInterceptor`), `EntityState.Added`/`Modified` durumundaki tüm `BaseEntity` kayıtlarında audit alanlarını doldurur. `IDateTimeProvider`/`ICurrentUserService` bağımlılıkları **opsiyoneldir** (Task 5/6'da implement edilecek); kayıtlı değillerse sırasıyla `DateTime.UtcNow` ve `null`'a düşer — gerçek implementasyonlar eklendiğinde interceptor'da değişiklik gerekmez (bkz. ADR-016). Soft-delete filtresi (`!IsDeleted`) `BaseEntityConfiguration` üzerinden tüm entity'lerde global olarak uygulanır.

### Migration ve Seed

Tasarım zamanı migration üretimi için `AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>` kullanılır (sabit SQL Server Express bağlantısıyla, WebAPI'nin DI container'ından bağımsız — bkz. DEVELOPMENT_RULES.md §5.3, komutlar Persistence projesinden çalıştırılır). `DbSeeder`, idempotent şekilde (ürünler tablosu doluysa atlar) 5 sigorta ürünü + teminatlarını, örnek bir müşteriyi ve örnek araç/teklif/poliçe zincirini (domain'in kendi state-machine metotlarıyla `Draft → Priced → Approved → Purchased`) seed eder. Sabit GUID'ler `Seed/SeedIds.cs`'te tanımlıdır (DEVELOPMENT_RULES.md §5.4); `SeedIds.SampleCustomerAppUserId`, Task 5'te seed edilecek örnek Identity kullanıcısının `Id`'siyle birebir eşleşmelidir.

`Program.cs`, development ortamında `Database.MigrateAsync()` + `DbSeeder.SeedAsync()` çağırır; bağlantı dizesi `appsettings.Development.json`'da yerel SQL Server Express instance'ı (`.\SQLEXPRESS`), `appsettings.json`'da (üretim) boş placeholder'dır — gerçek ortam değeri deployment sırasında sağlanır. Tasarım zamanı (`AppDbContextFactory`) bağlantıyı `SIGORTAPRO_DESIGN_CONNECTION` ortam değişkeninden okur, tanımlı değilse aynı yerel instance'a düşer.

## Kimlik Doğrulama & Yetkilendirme (JWT + Identity)

Task 5 ile ASP.NET Core Identity + JWT tabanlı kimlik doğrulama kuruldu (ADR-003, ADR-014). Katman dağılımı, "kimlik Domain dışıdır" ilkesini korur:

- **Persistence** — `AppUser : IdentityUser<Guid>` ve `RefreshToken` (`Identity/`); `AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>`. `IIdentityService` (kullanıcı oluşturma/doğrulama, `UserManager<AppUser>` tabanlı) ve `IRefreshTokenService` (refresh token saklama/rotasyon) implementasyonları burada. Identity `AddIdentityCore` + EF store'larıyla kurulur (cookie yok). `IdentitySeeder` rolleri, admin ve örnek müşteri kullanıcısını idempotent seed eder.
- **Infrastructure** — `ITokenService` implementasyonu `TokenService`: HS256 imzalı JWT access token (15 dk) + kriptografik rastgele refresh token (7 gün) üretir; `JwtSettings` konfigürasyonuyla çalışır ve yalnızca primitif değerler kullandığı için Persistence'a bağımlı değildir (Infrastructure ↛ Persistence korunur).
- **Application** — `IIdentityService` / `ITokenService` / `IRefreshTokenService` arayüzleri, `Features/Auth/` altında `Register` / `Login` / `RefreshToken` command + handler + validator'ları, `AuthResponse` DTO'su. Rol adları `Common/Authorization/Roles` sabitlerinde `UserRole` enum'undan türetilir. Beklenen auth hataları (yanlış kimlik → 401, duplicate e-posta → 409) `Result<AuthResponse>` ile taşınır. Ortak `TcknValidation` (algoritmik TCKN doğrulaması) burada tanımlıdır (Task 7 de kullanacak).
- **WebAPI** — `AuthController` (`api/v1/auth/{register,login,refresh-token}`), JWT bearer authentication + authorization pipeline (`ServiceCollectionExtensions.AddWebApiServices`), `ICurrentUserService` (HttpContext tabanlı — kaynak sahipliği ve audit için). `Program.cs` composition root tüm katman DI kayıtlarını birleştirir ve development ortamında migration + `DbSeeder` + `IdentitySeeder` çağırır.

**Kayıt atomikliği (ADR-017):** `RegisterCommandHandler`, Identity kullanıcısı + Domain `Customer` kaydını `IUnitOfWork.ExecuteInTransactionAsync` ile tek transaction'da oluşturur; biri başarısız olursa ikisi de geri alınır.

## API Çapraz Kesit Altyapısı (Cross-Cutting)

Task 6, prodüksiyon standardında bir API kabuğu kurar. Tüm bileşenler `SigortaPro.WebAPI` altında `Middleware/`, `Extensions/`, `HealthChecks/` klasörlerinde toplanır; DI ve pipeline kurulumu `ServiceCollectionExtensions` / `ApplicationBuilderExtensions` üzerinden merkezîdir.

### HTTP Pipeline Sırası

```
CorrelationId → SecurityHeaders → SerilogRequestLogging → ExceptionHandling
  → [Swagger (dev) | HSTS (prod)] → HttpsRedirection → Routing → CORS
  → RateLimiter → Authentication → Authorization → Controllers + /health
```

### Bileşenler

- **Global Exception Handling** (`ExceptionHandlingMiddleware`) — `SigortaProException` hiyerarşisini RFC 7807 `ProblemDetails`'e eşler: `ValidationException`→400 (alan bazlı `errors`), `NotFoundException`→404, `ForbiddenAccessException`→403, `BusinessRuleException`→409, taban tip→400, bilinmeyen→500 (iç detay yalnızca Development'ta açık). Yanıtlara `traceId` + `correlationId` eklenir; `application/problem+json`. `[ApiController]` otomatik model-binding doğrulaması da `InvalidModelStateResponseFactory` ile **aynı zarfa** hizalandı — böylece tüm doğrulama hataları tek formatta döner (ADR-018). Bu, Task 5'teki "middleware gelene kadar 500" boşluğunu kapatır.
- **Serilog** (ADR-011) — console + günlük rolling file (`logs/sigortapro-.log`, 7 gün); yapılandırma `appsettings.json > Serilog` bölümünden (`ReadFrom.Configuration`). Bootstrap logger, host kurulum hatalarını da yakalar (`try/catch/finally` + `Log.CloseAndFlush`). `CorrelationIdMiddleware` her isteğe `X-Correlation-ID` atar (gelen header korunur, yoksa üretilir), `LogContext` ile tüm loglara işler ve yanıt header'ında geri döndürür; `UseSerilogRequestLogging` istek özetini korelasyon kimliğiyle zenginleştirir.
- **CORS** — `Cors:AllowedOrigins` (varsayılan Vite `http://localhost:5173`); credentials + `X-Correlation-ID` expose edilir.
- **API Versiyonlama** — konvansiyon tabanlı URL yaklaşımı (`/api/v1/...`, controller'lar `Controllers/v1/`), harici kütüphane olmadan (ADR-019).
- **Swagger/OpenAPI + JWT** — Swashbuckle 6.6.2 (.NET 8 uyumlu), Bearer güvenlik şeması + XML doc yorumları; yalnızca Development'ta.
- **Health Check** — `GET /health`, JSON yanıt; `DatabaseHealthCheck` EF Core `CanConnectAsync` ile DB'yi yoklar (ek NuGet paketi yok).
- **Rate Limiting & Güvenlik Header'ları** (ADR-020) — yerleşik ASP.NET Core 8 rate limiter: auth uçlarına IP başına 10 istek/dakika (fixed window, aşımda 429). `SecurityHeadersMiddleware` (`OnStarting`): `X-Content-Type-Options`, `X-Frame-Options: DENY`, `Referrer-Policy`, `Permissions-Policy`; HSTS yalnızca üretimde.

## İş Modülleri (FAZ 1)

### Müşteri & Profil Modülü (Task 7)

İlk iş modülü, Task 3–6'da kurulan CQRS + cross-cutting omurgasının üzerine oturur. Tüm bileşenler `SigortaPro.Application/Features/Customers/` altında CQRS klasör düzenine (`Commands/`, `Queries/`, `DTOs/`) uyar; HTTP yüzeyi `CustomersController` (`Controllers/v1/`).

**Uçlar:**

| Uç | İşlem | Yetki |
|----|-------|-------|
| `GET /api/v1/customers/me` | Oturum sahibinin profili (araç/konut ile) | `Customer` |
| `PUT /api/v1/customers/me` | Ad/soyad, telefon, adres güncelleme | `Customer` |
| `POST /api/v1/customers/me/vehicles` | Araç ekleme | `Customer` |
| `PUT /api/v1/customers/me/vehicles/{vehicleId}` | Araç güncelleme (sahiplik kontrollü) | `Customer` |
| `POST /api/v1/customers/me/properties` | Konut ekleme | `Customer` |
| `GET /api/v1/customers` | Müşteri listesi (sayfalama + arama + il filtresi) | `Admin`, `Personel` |
| `GET /api/v1/customers/{id}` | Müşteri detayı | `Admin`, `Personel` |

**Öne çıkan kararlar:**

- **Modüle özgü repository:** Application katmanı EF Core'a bağımlı olamadığından (Include, arama, async materialization EF gerektirir), `ICustomerRepository : IReadRepository<Customer>, IWriteRepository<Customer>` arayüzü Application'da tanımlanır; `CustomerRepository` (generic `GenericRepository<Customer>`'dan türeyerek) Persistence'ta implement eder. Bu, ARCHITECTURE_RULES.md §4.2'nin "modüle özgü karmaşık sorgular için özel repository interface'i" izniyle ADR-005 kapsamındadır — yeni bir ADR gerektirmez.
- **Kaynak sahipliği:** Müşteri uçları `ICurrentUserService.UserId` (= `AppUserId`) üzerinden müşteriyi çözümler; `me` uçlarında sahiplik içkindir. Araç güncelleme ayrıca `vehicle.CustomerId == currentCustomer.Id` kontrolüyle başka müşterinin kaydını 403'e düşürür (DEVELOPMENT_RULES.md §7).
- **Hassas veri:** Tüm müşteri DTO'ları ham TCKN yerine `SensitiveDataMasker.MaskTckn` ile maskeli değer taşır (CODING_STANDARDS.md §4.2, §8.3).
- **Hata sözleşmesi:** Query/command handler'ları not-found → `NotFoundException` (404), yetkisiz kaynak → `ForbiddenAccessException` (403) fırlatır; ADR-018 middleware'i bunları RFC 7807 ProblemDetails'e eşler. `Result<T>` sarmalayıcısı yalnızca beklenen soft-fail'i (401/409) olan Auth akışında kullanılır.
- **Doğrulama:** Ortak regex kalıpları `Common/Validation/ValidationPatterns` (Türk plakası, telefon) altında toplanır; risk objesi alanları (üretim yılı, motor gücü, bina yaşı, metrekare, deprem bölgesi) aralık kontrolleriyle doğrulanır.

### Risk Analizi & Fiyatlama Motoru (Task 8)

Kural tabanlı mock fiyatlama motoru (ADR-008). Arayüz `IPricingEngine` Application'da (`Common/Interfaces`), implementasyon `PricingEngine` Infrastructure'da (`Services/Pricing/`); ARCHITECTURE_RULES.md §6.1'e uyar. DI: Scoped (§6.2).

**Tasarım ilkeleri:**

- **Saf/deterministik fonksiyon:** `CalculatePremium(PricingRequest) → PricingResult`. Motor sistem saatine (`DateTime.Now`), rastgeleliğe, domain entity'lerine veya Quote akışına bağımlı değildir; girdiler önceden hesaplanmış primitiflerdir (sürücü/araç yaşı vb.). Bu, motoru Task 9'daki Quote akışından **bağımsız, yeniden kullanılabilir ve tam izole test edilebilir** kılar (kullanıcı gereksinimi).
- **Polimorfik istek:** `PricingRequest` soyut kaydından türeyen `VehiclePricingRequest` (Kasko/Trafik), `PropertyPricingRequest` (Konut/DASK), `HealthPricingRequest` (Sağlık). Motor, istek tipine göre `switch` ile dallanır; branş ile risk objesi tipi uyuşmazsa `ArgumentException` fırlatır.
- **Çıktı:** `PricingResult` = baz prim × faktör çarpanları → `TotalPremium` (2 ondalık), `RiskScore` (Low/Medium/High, toplam çarpan eşiğine göre) ve `PricingBreakdownItem[]` (faktör adı, çarpan, Türkçe açıklama).
- **Kural/veri ayrımı:** Fiyatlama sözleşmeleri Application'da (`Common/Pricing/`); baz primler, il katsayıları, eşikler ve hasarsızlık parametreleri Infrastructure'daki `PricingRuleTables`'da merkezîdir. Tüm kural değerleri kök dizindeki [`PRICING.md`](PRICING.md) ile birebir eşleşir.

### Teklif (Quote) Modülü (Task 9)

Teklif akışı, Task 8 fiyatlama motorunu tüketerek teklif oluşturma, listeleme, detay, durum geçişleri ve karşılaştırma sağlar. Bileşenler `Application/Features/Quotes/` altında CQRS düzeninde; HTTP yüzeyi `QuotesController`.

**Uçlar:**

| Uç | İşlem | Yetki |
|----|-------|-------|
| `POST /api/v1/quotes` | Branş + risk objesi + paket ile teklif oluştur (fiyatla) | `Customer` |
| `GET /api/v1/quotes/compare` | Teminat seviyeli 2-3 paket önizlemesi | `Customer` |
| `GET /api/v1/quotes` | Teklif listesi (müşteri kendi / personel tümü, filtre+sayfalama) | Kimliği doğrulanmış |
| `GET /api/v1/quotes/{id}` | Teklif detayı (prim dökümü ile) | Sahip müşteri / personel |
| `POST /api/v1/quotes/{id}/approve` | Priced → Approved | `Customer` |
| `POST /api/v1/quotes/{id}/reject` | → Rejected | `Customer` |

**Öne çıkan kararlar:**

- **Fiyatlama köprüsü (`QuotePricingFactory`):** Domain verisinden Task 8 motorunun saf girdisini (`VehiclePricingRequest`/`PropertyPricingRequest`/`HealthPricingRequest`) kurar, teminat paketi ölçeğini uygular ve prim dökümü + ölçekli teminatları üretir. Sürücü/araç yaşı `IDateTimeProvider` referansıyla hesaplanır; motor sistem saatinden bağımsız kalır.
- **Seçim saklanır, çıktı saklanmaz (ADR-021):** `Quote`'a yalnızca `CoveragePackage` alanı eklendi. Prim dökümü ve risk skoru kalıcı değildir; teklif detayında saklı seçim + `Quote.CreatedAt` referansıyla deterministik yeniden hesaplanır. Gösterilen toplam otoriter olarak saklı `TotalPremium`'dur.
- **Teminat paketleri:** Standart/Genişletilmiş/Premium seviyeleri primi ve teminat limitlerini ölçekler (`CoveragePackageFactors`, [`PRICING.md`](PRICING.md) "Teminat Paketleri"). Paket, risk skorunu etkilemez (kapsam seçimidir, risk değil).
- **Durum makinesi:** Geçişler domain `Quote` metotlarında (`MarkAsPriced`/`Approve`/`Reject`); geçersiz geçiş `DomainException` → 409 (ADR-013). Onayda geçerlilik süresi kontrol edilir; otomatik `Expired`'a çekme Task 13 arkaplan servisine bırakıldı.
- **Kaynak sahipliği (`QuoteAuthorization`):** Teklifin `CustomerId`'si ile çağıranın müşteri kaydı karşılaştırılır (EF navigasyonundan bağımsız → test edilebilir); Admin/Personel muaftır.
- **Repository'ler:** `IQuoteRepository` (detay/tracked/arama Include'ları), `IInsuranceProductRepository` (branşa göre aktif ürün + teminatlar) — ARCHITECTURE_RULES.md §4.2 / ADR-005.

### Ödeme & Poliçeleştirme Modülü (Task 10)

Onaylanmış teklif, mock sanal POS ile ödenip poliçeleştirilir. Bileşenler `Application/Features/Payments/` altında CQRS düzeninde; HTTP yüzeyi `PaymentsController`. Ödeme gateway'i (`IPaymentService`) Application'da soyut, `MockVirtualPosService` Infrastructure'da (ADR-007).

**Uçlar:**

| Uç | İşlem | Yetki |
|----|-------|-------|
| `POST /api/v1/payments` | Onaylı teklifi öde → başarılıysa poliçe oluştur (Approved → Purchased) | `Customer` |
| `GET /api/v1/payments` | Ödeme geçmişi (başarılı/başarısız denemeler, sayfalı) | `Customer` |
| `GET /api/v1/payments/installment-options` | Onaylı teklifin primi için taksit planları (faizsiz mock) | `Customer` |

**Öne çıkan kararlar:**

- **Mock sanal POS (`MockVirtualPosService`, ADR-007):** Kartı Luhn ile doğrular; senaryo test kartları başarısız sonuç üretir (`4000000000000002` → yetersiz bakiye, `4000000000000069` → 3D Secure hatası), diğer geçerli kartlar başarılı. Saf/deterministik; ham kart log'lanmaz. Kart maskeleme gateway'in sorumluluğundadır (`SensitiveDataMasker.MaskCardNumber` — yalnızca son 4 hane).
- **Atomik poliçeleştirme (ADR-017, ADR-022):** Başarılı ödemede `Payment` (Successful) + `Policy` üretimi + `Quote.Purchase()` tek transaction'da yürür. Başarısız ödemede yalnızca `Payment` (Failed) kaydedilir, poliçe/teklif değişmez ve `PaymentFailedException` → 402 döner.
- **Poliçe numarası (`PolicyNumberFactory`, ADR-022):** `POL-{yıl}-{6 haneli sıra}` (örn. `POL-2026-000002`). Sıra, yıla ait mevcut poliçe sayısından (soft-delete dahil, `IgnoreQueryFilters`) türetilir; benzersizliği `UQ_Policies_PolicyNumber` unique index garanti eder.
- **Kaynak sahipliği:** `QuoteAuthorization` (Task 9) ödeme/taksit uçlarında yeniden kullanılır; müşteri yalnızca kendi teklifini öder, ödeme geçmişi kendi kayıtlarıyla sınırlıdır.
- **Şema:** Yeni migration gerekmez — `Payments`/`Policies` tabloları Task 4 `InitialCreate` ile mevcuttur. Repository'ler: `IPaymentRepository`, `IPolicyRepository` (ADR-005 §4.2).

### PDF Poliçe Dökümanı Modülü (Task 11)

Satın alınan poliçe için QuestPDF (ADR-006) ile sertifika üretilir, saklanır ve sahiplik kontrolüyle indirilir. Bileşenler `Application/Features/Policies/` (CQRS) + `Infrastructure/Services/{Documents,Storage}/`; HTTP yüzeyi `PoliciesController`.

**Uç:**

| Uç | İşlem | Yetki |
|----|-------|-------|
| `GET /api/v1/policies/{id}/document` | Poliçe sertifikası PDF'ini indir (ilk erişimde üretilir) | Sahip müşteri / personel |

**Öne çıkan kararlar:**

- **Lazy + idempotent üretim (ADR-023):** PDF ilk indirmede yoksa üretilip diske yazılır ve `PolicyDocument` kaydı oluşturulur; sonrakiler saklanan dosyayı döner. Ödeme akışı (Task 10) belge I/O'suna bağlanmaz (SRP; transaction içinde dosya I/O ve catch-all yok).
- **Katman ayrımı:** `IPolicyDocumentService` (saf render — modelden PDF baytları) ve `IFileStorageService` (göreli anahtarla saklama) Application'da soyut, Infrastructure'da implement (`PolicyPdfDocumentService`, `LocalFileStorageService`). Depolama blob'a hazırdır; path-traversal engellenir. QuestPDF Community lisansı DI kaydında bir kez ayarlanır; render/saklama servisleri stateless → Singleton.
- **Deterministik döküm:** Sertifikadaki prim dökümü + ölçekli teminatlar, Task 9 `QuotePricingFactory` ile teklifin `CreatedAt` referansından yeniden hesaplanır (ADR-021 — teklif detayı ve poliçe aynı primi üretir). TCKN maskeli basılır.
- **Sahiplik:** `QuoteAuthorization` (Task 9) yeniden kullanılır — müşteri kendi poliçesi; Admin/Personel muaf.
- **EF child-insert:** `PolicyDocument`, istemci-üretimli Guid anahtarı nedeniyle `IWriteRepository<PolicyDocument>.AddAsync` ile explicit eklenir (aksi halde EF, navigation üzerinden bırakılan child'ı Modified sanıp 0-satır UPDATE'e düşer — ADR-023). Yeni migration gerekmedi (`PolicyDocuments` tablosu Task 4 ile mevcut).

### Hasar (Claim) Modülü (Task 12)

İki taraflı hasar süreci: müşteri bildirir, acente personeli inceleyip karara bağlar ve öder. Bileşenler `Application/Features/Claims/` altında CQRS düzeninde; HTTP yüzeyi `ClaimsController`. `Claim` entity'si ve durum makinesi (`Submitted → UnderReview → Approved/Rejected → Paid`) Task 2'de tanımlıdır; `Claims` tablosu Task 4 `InitialCreate` ile mevcuttur (yeni migration gerekmedi).

**Uçlar:**

| Uç | İşlem | Yetki |
|----|-------|-------|
| `POST /api/v1/claims` | Hasar bildirimi (aktif poliçe + dönem içi olay + mock foto) | `Customer` |
| `GET /api/v1/claims` | Hasar listesi (müşteri kendi / personel tümü, filtre+sayfalama) | Kimliği doğrulanmış |
| `GET /api/v1/claims/{id}` | Hasar detayı | Sahip müşteri / personel |
| `POST /api/v1/claims/{id}/start-review` | Submitted → UnderReview | `Admin`, `Personel` |
| `POST /api/v1/claims/{id}/approve` | UnderReview → Approved (onay tutarı + not) | `Admin`, `Personel` |
| `POST /api/v1/claims/{id}/reject` | UnderReview → Rejected (gerekçe) | `Admin`, `Personel` |
| `POST /api/v1/claims/{id}/pay` | Approved → Paid | `Admin`, `Personel` |

**Öne çıkan kararlar (ADR-024):**

- **Dönem/aktiflik iş kuralı:** Hasar yalnızca **aktif** poliçeye, poliçe **dönemi içindeki** ve gelecekte olmayan bir olaya açılabilir. Kural cross-aggregate (Claim ↔ Policy) + saat (`IDateTimeProvider`) gerektirdiğinden `CreateClaimCommandHandler`'da `BusinessRuleException` (409) ile uygulanır; yapısal doğrulamalar FluentValidation'da (400).
- **Mock foto yükleme:** `CreateClaimCommand.PhotoFileNames` istemci metadatası kabul edilip doğrulanır (uzantı/sayı) ancak **saklanmaz** — gerçek hasar fotoğraf depolama MVP dışıdır (PROJECT_CONTEXT §9); domain/şema değişmez, yüzey ileride gerçek depolamaya hazırdır (ADR-023 deseni).
- **Durum makinesi:** Geçişler domain `Claim` metotlarında (`StartReview`/`Approve`/`Reject`/`MarkPaid`); geçersiz geçiş `DomainException` → 409 (ADR-013).
- **Fiyatlama beslemesi (Task 13 seam'i):** `IClaimRepository.CountReportableClaimsByCustomerAsync` müşterinin `Approved`+`Paid` hasar sayısını döndürür; yenileme fiyatlaması (Task 13) bunu hasarsızlık basamağı/çarpana besler (motor `NoClaimTier` ile zaten uyumlu). Task 12 çarpanı uygulamaz.
- **Kaynak sahipliği:** `QuoteAuthorization` (Task 9) yeniden kullanılır — müşteri yalnızca kendi hasarına erişir; Admin/Personel muaf. Repository: `IClaimRepository` (ADR-005 §4.2).

### Poliçe Yenileme (Renewal) & Arkaplan İşleri (Task 13)

Yaşayan poliçe yaşam döngüsü: bir arkaplan servisi süresi geçen teklif/poliçeleri `Expired`'a çeker ve bitişine ≤30 gün kalan poliçeler için otomatik yenileme teklifi üretir; müşteri teklifi görüp onaylar ve mevcut ödeme akışıyla yeni dönem poliçesini oluşturur. Bileşenler `Application/Features/Renewals/` (+ `Features/Quotes|Policies/Commands/Expire*`) ve `Infrastructure/BackgroundJobs`; HTTP yüzeyi `RenewalsController`.

**Arkaplan servisi:** `PolicyLifecycleBackgroundService` (`BackgroundService`) başlangıçta + 6 saatte bir çalışır; her çalışmada `IServiceScopeFactory` ile scope açıp `ISender` üzerinden üç komut gönderir. Servis **iş kuralı içermez** (Clean Architecture); yalnızca zamanlama + scope yönetimi yapar.

**Uçlar:**

| Uç | İşlem | Yetki |
|----|-------|-------|
| `GET /api/v1/renewals` | Müşterinin yenileme teklifleri (sayfalı) | `Customer` |
| `POST /api/v1/renewals/{id}/accept` | Yenilemeyi onayla → yeni teklif Approved (ödemeye hazır) | `Customer` |

**Öne çıkan kararlar (ADR-025):**

- **Süre dolumu + yenileme üretimi CQRS komutlarında:** `ExpireOutdatedQuotesCommand` (Draft/Priced/Approved + süresi dolmuş → `Quote.Expire`), `ExpireOverduePoliciesCommand` (aktif + bitiş geçmiş → `Policy.ExpireIfPastEndDate`), `GeneratePolicyRenewalsCommand` (pencere içi + `!Renewals.Any()` → idempotent). Task 9'da bu servise bırakılan teklif-expiry burada tamamlandı.
- **Hasar geçmişi çarpanı teklifte saklanır:** Müşterinin fiyatlamaya etki eden (`Approved`+`Paid`) hasar sayısı (Task 12 seam'i `CountReportableClaimsByCustomerAsync`) → `RenewalPricing` ile çarpana eşlenir (her hasar +%20, tavan +%60 — [`PRICING.md`](PRICING.md) §6.1) ve `Quote.ClaimHistoryFactor` (additive alan; migration `AddQuoteClaimHistoryFactor`, varsayılan 1.00) olarak saklanır. `QuotePricingFactory` bu saklı girdiyi `CoveragePackage` gibi kullanır → prim dökümü deterministik yeniden hesaplanır (ADR-021 korunur; normal teklifler 1.00 → davranış değişmez).
- **Onay mevcut akışa bağlanır (DRY):** `AcceptRenewalCommand`, `Renewal.Accept()` + yeni teklif `Quote.Approve()` işlemlerini tek transaction'da yapar; ödeme/poliçeleştirme **mevcut** `PurchaseQuoteCommand` (Task 10) ile yürür. Sahiplik `QuoteAuthorization` (Task 9) ile.
- **Mock bildirim:** `INotificationService` (Application) + `MockNotificationService` (Infrastructure, log/e-posta simülasyonu). Repository: `IRenewalRepository` (ADR-005 §4.2). NuGet: `Microsoft.Extensions.Hosting.Abstractions` (HostedService için).

### Admin Dashboard & Raporlama Modülü (Task 14)

Acente personelinin admin panelinin **salt okunur** veri kaynağı: özet metrikler ve tarih aralıklı raporlar. Bileşenler `Application/Features/Dashboard/`; HTTP yüzeyi `DashboardController` (tümü `[Authorize(Roles = Staff)]`). Hiçbir uç durum değiştirmez; yeni migration yoktur.

| Uç | Açıklama | Yetki |
|----|----------|-------|
| `GET /api/v1/dashboard/summary` | Özet metrikler + oranlar + aylık trend + branş dağılımı | `Staff` |
| `GET /api/v1/dashboard/reports/policies` | Tarih aralıklı poliçe raporu (sayfalı) | `Staff` |
| `GET /api/v1/dashboard/reports/payments` | Tarih aralıklı ödeme raporu (sayfalı) | `Staff` |
| `GET /api/v1/dashboard/reports/riskiest-customers` | En riskli müşteri segmentleri (ilk N) | `Staff` |

- **Özel salt okunur repository:** `IDashboardRepository` (ADR-005 §4.2, ADR-026) tüm metrikleri **SQL tarafı agregasyonla** (`CountAsync`/`SumAsync`/`GroupBy`, `AsNoTracking`) üretir — entity materialize etmez, N+1 üretmez. Metrikler mevcut modül repository'lerine dağıtılmadı (çalışan modüllere dokunulmadı, okuma tarafı kohezyonlu tutuldu).
- **Read-model'ler (DTO değil):** Gruplu metrikler `MonthlySalesAggregate`/`BranchDistributionAggregate`/`CustomerRiskAggregate` read-model kayıtları döner (`Features/Dashboard/ReadModels`); handler bunları `DashboardMappings` ile API DTO'suna manuel eşler → §4.2 ("repository DTO döndürmez") korunur. Rapor listeleri (poliçe/ödeme) entity + `Include` döner, handler map eder (Task 7–13 deseni).
- **Türetilmiş oranlar Application'da:** Yenileme oranı (onaylanan/sunulan) ve hasar/prim oranı (ödenen hasar/üretilen prim) `GetDashboardSummaryQueryHandler` içinde 0'a bölme korumasıyla hesaplanır; aylık trend penceresi (son 12 ay) `IDateTimeProvider`'dan türetilir.

## Mimari Kararlar

Tüm önemli mimari kararlar ve gerekçeleri için bkz. [`docs/ai/DECISIONS.md`](docs/ai/DECISIONS.md). Bu doküman kuruluşta ADR-001 (Clean Architecture katman yapısı) kararını uygular; Domain modeli ADR-013 ve ADR-014 kararlarını, Application/CQRS omurgası ADR-002, ADR-013 ve ADR-015 kararlarını, Persistence katmanı ADR-005, ADR-010 ve ADR-016 kararlarını, kimlik doğrulama ADR-003, ADR-014 ve ADR-017 kararlarını, cross-cutting API altyapısı ADR-018 (exception handling/ProblemDetails), ADR-019 (API versiyonlama) ve ADR-020 (rate limiting + güvenlik header'ları) kararlarını uygular (ADR-012, ADR-014 ile revize edilmiştir).

## Durum

Bu doküman **Task 1 — Solution İskeleti**, **Task 2 — Domain Katmanı**, **Task 3 — Application Katmanı Altyapısı (CQRS Çekirdeği)**, **Task 4 — Persistence Katmanı (EF Core + SQL Server)**, **Task 5 — Kimlik Doğrulama & Yetkilendirme (JWT + Roller)** ve **Task 6 — API Çapraz Kesit Altyapısı (Cross-Cutting)** kapsamında oluşturulmuş; FAZ 1 iş modülleri **Task 7 — Müşteri & Profil Modülü**, **Task 8 — Risk Analizi & Dinamik Fiyatlama Motoru (Mock)**, **Task 9 — Teklif (Quote) Modülü**, **Task 10 — Ödeme (Mock Sanal POS) & Poliçeleştirme**, **Task 11 — PDF Poliçe Dökümanı Üretimi**, **Task 12 — Hasar (Claim) Modülü**, **Task 13 — Poliçe Yenileme (Renewal) & Arkaplan İşleri** ve **Task 14 — Admin Dashboard & Raporlama API'si** ile tamamlanmıştır. FAZ 0 ve FAZ 1 (backend çekirdek iş modülleri) tamamlanmıştır; sonraki adım **FAZ 2 — Task 15 — React Proje Kurulumu & Tasarım Sistemi**'dir.
