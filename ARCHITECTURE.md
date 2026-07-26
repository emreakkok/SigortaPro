# SigortaPro — Mimari Doküman

> API uçlarının derli toplu özeti için bkz. [`API.md`](API.md); kurulum ve çalıştırma için [`README.md`](README.md).

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
Domain dışı teknik servisler: JWT token üretimi, mock fiyatlama motoru, mock sanal POS, QuestPDF ile poliçe dokümanı üretimi, mock bildirim servisi, SMTP e-posta gönderimi (MailKit — Task 23, ADR-035), gömülü JSON araç kataloğu sağlayıcısı (Task 24, ADR-036), gömülü JSON il kataloğu sağlayıcısı (Post-MVP, ADR-037), SignalR gerçek zamanlı bildirim hub'ı + yayıncısı (ADR-041; kalıcı bildirim fan-out orkestrasyonu Application'daki NotificationDispatcher'dadır — ADR-042), arkaplan işleri. Klasör iskeleti: `Services/`, `BackgroundJobs/`, `Email/`, `RealTime/`.

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

**Şifre sıfırlama (Task 23, ADR-035):** `AuthController`'a additive iki `[AllowAnonymous]` uç eklendi (`forgot-password`, `reset-password`); mevcut rate limit politikası (ADR-020) otomatik uygulanır. Reset token'ları ASP.NET Core Identity'nin `DataProtectorTokenProvider`'ıyla üretilir (`AddDefaultTokenProviders`, `DataProtectionTokenProviderOptions.TokenLifespan = 1 saat` — Persistence DI'de; bu provider'lar ASP.NET Core paylaşımlı framework'te olduğundan Persistence'a `FrameworkReference Microsoft.AspNetCore.App` eklendi). `IIdentityService` reset token üret/uygula metotlarıyla genişletildi. E-posta gönderimi soyut `IEmailService` (Application) → `SmtpEmailService` (Infrastructure, MailKit) ile; şifre-sıfırlamaya özgü link/şablon kompozisyonu `IPasswordResetNotifier` → `PasswordResetNotifier` (INotificationService deseninin izi). `forgot-password` kullanıcı varlığını sızdırmaz (her koşulda generic 200; SMTP hatası tiplenmiş `EmailDeliveryException` olarak yutulur). Mevcut JWT/refresh mimarisi, auth uçları/DTO'ları ve şema değişmedi; migration yoktur.

**Admin/Personel yetki ayrımı ve personel yaşam döngüsü (ADR-060, ADR-061):** Yetki modeli **role-based** kalır (permission sistemi yok). "Acente çalışanı kümesi" (Admin ∪ Personel) tek kaynaktan yönetilir: attribute'ler `Roles.Staff` string sabitini, çalışma zamanı kontrolleri `Roles.StaffRoles` dizisini kullanır (önceki 4 duplike `IsInRole` kontrolü buna bağlandı). Admin'e özel yüzeyler: **personel yönetimi** (`api/v1/staff` — 5 uç, `[Authorize(Roles = Admin)]`), **fiyatlandırma** (ADR-048), **hasar ödemesi** (`claims/{id}/pay`) ve **ödeme/ciro raporu** (`dashboard/reports/payments`). Personel bu uçlarda 403 alır ama operasyonu (müşteri/teklif/poliçe/hasar inceleme + onay/ret + dashboard özeti) sürdürür. **Güvenlik değişmezleri:** Staff API rolü istemciden almaz (daima `Personel`'e sabit — mass-assignment savunması); Admin oluşturma/rol değiştirme/silme yüzeyi yoktur; `SetStaffActiveAsync` yalnızca `Personel` hedefler → son-Admin invariant'ı yapısal olarak korunur; `staff/{id}` hedef Personel değilse 404 (IDOR savunması). **Hesap yaşam döngüsü:** `AppUser.IsActive` (+`FullName`) additive migration ile eklendi (mevcut satırlar `DEFAULT 1`); pasif hesap login ve refresh yapamaz (aktiflik sızdırılmaz), pasifleştirmede tüm refresh token'lar iptal edilir (`RevokeAllForUserAsync`) → en kötü erişim penceresi access token ömrü kadardır (≤ 15 dk; blacklist kurulmadı). Fiyatlandırma yolu (ADR-021/048/053-059) etkilenmedi. Ayrıntılı karar dosyası: `docs/ai/STAFF_ROLE_AUTHORIZATION_PLAN.md` §26.

**Dashboard finansal görünürlük ayrımı (ADR-062):** Admin/Personel ayrımı, yetki (endpoint erişimi) yanında **veri görünürlüğüne** de genişletilir. Yetki modeli **role-based** kalır (permission sistemi eklenmez). İlke: *kayıt-başına finansal alan (tek teklif/poliçe primi) operasyoneldir → Personel görür; agregat finansal metrik (toplam ciro/kârlılık/portföy primi/tahsilat) yönetimseldir → Admin-only.* `GET /dashboard/summary` **tek endpoint / tek sorgu** olarak kalır (ADR-052 korunur); `GetDashboardSummaryQueryHandler`, çağıranın rolüne göre (`ICurrentUserService.IsInRole(Admin)`) agregat finansal alanları **Personel için `null` maskeler** — böylece finansal veri response'a hiç yazılmaz. **Kritik ilke:** finansal veri yalnızca frontend'de gizlenmez; **backend'de de maskelenir** (frontend gizleme UX'tir, güvenlik değil). İlgili DTO alanları nullable yapıldı (additive; Admin için değerler birebir dolu döner). Frontend, Personel için finansal kartları `useRoles().isAdmin` ile hiç render etmez. `dashboard/reports/riskiest-customers` (hasar tutarı + müşteri profilleme) Admin-only'e alındı; `dashboard/reports/policies` Personel'e açık kaldı (kayıt-başına prim). ADR-052 tek-uç/tek-sorgu ve ADR-060/061 kararları geri alınmadı — genişletildi; migration gerekmedi.

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
- **Tarih-saat / timezone (ADR-063)** — **UTC-merkezli** strateji: instant değerler DB'de UTC (`datetime2`) saklanır; backend/domain yalnızca UTC ile çalışır (`IDateTimeProvider.UtcNow`). Merkezî EF value converter (`UtcDateTimeConverters`, `AppDbContext.ApplyUtcDateTimeConversions`) instant DateTime alanlarını **okuma anında `Kind=Utc`** işaretler → System.Text.Json ISO-8601 + **"Z"** üretir (EF `datetime2`→`Kind=Unspecified` materializasyonu + System.Text.Json'ın Kind'a bağlı "Z" davranışının yol açtığı "13:23→10:23" kök nedeni çözülür). **Date-only** alanlar (`BirthDate`) converter'dan hariçtir (takvim günü — tz'siz). Timezone yalnızca bir **sunum** meselesidir: frontend `Europe/Istanbul` ile gösterir (`APP_TIME_ZONE`). `DateTimeOffset`'e geçilmedi (tek-tz için gereksiz); migration/backfill gerekmedi.
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
| `GET /api/v1/policies` | "Poliçelerim": müşterinin poliçeleri (sayfalı + durum filtresi) — **Task 18, ADR-031** | `Customer` |
| `GET /api/v1/policies/{id}` | Poliçe detayı (teminat tablosu ile) — **Task 18, ADR-031** | Sahip müşteri / personel |
| `GET /api/v1/policies/{id}/document` | Poliçe sertifikası PDF'ini indir (ilk erişimde üretilir) | Sahip müşteri / personel |

> **Task 18 (ADR-031):** `GET /policies` ve `GET /policies/{id}` **salt okunur, additive** uçlardır (frontend "Poliçelerim" ekranı için). Mevcut desenleri izler: `IPolicyRepository.GetByCustomerPagedAsync`/`GetReadDetailByIdAsync` (AsNoTracking; PDF'in tracked `GetDetailByIdAsync`'inden ayrı), `QuoteAuthorization` sahiplik, `QuotePricingFactory` ile deterministik teminat yeniden hesabı (ADR-021). Hiçbir mevcut uç/DTO/şema değişmedi; migration yok.

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
| `POST /api/v1/claims/{id}/pay` | Approved → Paid | **Yalnızca `Admin`** (ADR-060) |

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
| `GET /api/v1/dashboard/summary` | Operasyon dashboard'ı — tüm bloklar tek çağrıda | `Staff` — **agregat finansal alanlar Personel'e backend'de null maskelenir** (ADR-062) |
| `GET /api/v1/dashboard/reports/policies` | Tarih aralıklı poliçe raporu (sayfalı; kayıt-başına prim Personel'e açık — ADR-062 D2) | `Staff` |
| `GET /api/v1/dashboard/reports/payments` | Tarih aralıklı ödeme/ciro raporu (sayfalı) | **Yalnızca `Admin`** (ADR-060) |
| `GET /api/v1/dashboard/reports/riskiest-customers` | En riskli müşteri segmentleri (ilk N) | **Yalnızca `Admin`** (ADR-062 D3) |

- **Özel salt okunur repository:** `IDashboardRepository` (ADR-005 §4.2, ADR-026) tüm metrikleri **SQL tarafı agregasyonla** (`CountAsync`/`SumAsync`/`GroupBy`, `AsNoTracking`) üretir — entity materialize etmez, N+1 üretmez. Metrikler mevcut modül repository'lerine dağıtılmadı (çalışan modüllere dokunulmadı, okuma tarafı kohezyonlu tutuldu).
- **Read-model'ler (DTO değil):** Gruplu metrikler `MonthlySalesAggregate`/`BranchDistributionAggregate`/`CustomerRiskAggregate` read-model kayıtları döner (`Features/Dashboard/ReadModels`); handler bunları `DashboardMappings` ile API DTO'suna manuel eşler → §4.2 ("repository DTO döndürmez") korunur. Rapor listeleri (poliçe/ödeme) entity + `Include` döner, handler map eder (Task 7–13 deseni).
- **Türetilmiş oranlar Application'da:** Yenileme oranı (onaylanan/sunulan) ve hasar/prim oranı (ödenen hasar/üretilen prim) `GetDashboardSummaryQueryHandler` içinde 0'a bölme korumasıyla hesaplanır; aylık trend penceresi (son 12 ay) `IDateTimeProvider`'dan türetilir.

## Frontend (React SPA) — FAZ 2

Task 15 ile `frontend/` altında SPA iskeleti kuruldu (ADR-009, ADR-027, ADR-028). Backend'den tamamen ayrık yaşar; tek temas noktası `VITE_API_BASE_URL` üzerinden HTTP API'dir (CORS `http://localhost:5173`'e açık — Task 6).

### Stack ve Yapı

| Katman | Teknoloji |
|--------|-----------|
| UI | React 18 + TypeScript (strict, `any` yasak) |
| Build | Vite 8 (`npm run build` = `tsc` tip kontrolü + `vite build`) |
| Routing | React Router v6 — `createBrowserRouter`, route bazlı `React.lazy` + `Suspense` |
| Server state | TanStack Query v5 (tek `queryClient`, staleTime 30 sn) |
| HTTP | Axios (tek instance; JWT interceptor + refresh yenileme) |
| Form | React Hook Form + Zod (`@hookform/resolvers`) — ilk ekran Task 16'da |
| Styling | Tailwind CSS v3 + shadcn/ui tarzı el yazımı bileşen seti (cva + tailwind-merge) |
| Grafikler | Recharts 2.15 (admin dashboard — yalnızca lazy dashboard chunk'ında; ADR-033) |

Klasör düzeni CODING_STANDARDS.md §2.3 ile birebir: `src/app/` (App, providers, routes, `ProtectedRoute`/`GuestRoute`, sistem sayfaları), `src/features/` (iş modülleri: `auth`, `profile`, `quotes`, `payments`, `policies`, `claims`, `renewals`, `dashboard`, `customers` — her feature kendi müşteri ve personel/admin yüzeyine sahiptir, ADR-033), `src/shared/` (`components/` tasarım sistemi, `layouts/` CustomerLayout + AdminLayout, `theme/` merkezi tema yönetimi — ADR-043, `hooks/` useDebounce, `lib/` axios/queryClient/session/apiError/env/cn, `types/` ortak API-auth-insurance tipleri, `utils/` validation + format), `src/styles/globals.css` (HSL CSS değişkenli tema token'ları, koyu tema hazır).

### Öne Çıkan Kararlar

- **Oturum & token yenileme (ADR-028):** Oturum (`userId/email/roles/accessToken/refreshToken`) localStorage'da tek anahtar altındadır; roller JWT decode edilmeden `AuthResponse.roles`'tan alınır. Axios yanıt interceptor'ı 401'de **tek uçuşlu (single-flight)** refresh yapar (rotasyonlu refresh token ile uyum için zorunlu), isteği bir kez tekrarlar, yenileme başarısızsa oturumu temizleyip `/401` sayfasına tam sayfa yönlendirir.
- **Oturum yönetimi (ADR-029, Task 16):** localStorage kalıcı doğruluk kaynağıdır; `AuthProvider` (`features/auth`) onun React yansımasıdır (`signIn`/`signOut`). Axios'un sessiz yenilemesi yalnızca localStorage'ı günceller (UI token okumadığından zararsız); zorunlu çıkıştaki tam sayfa yeniden yükleme state'i sıfırlar. Login/register RHF + Zod formlarıyla `features/auth` altındadır; Zod şemaları backend FluentValidation kurallarını Türkçe mesajlarıyla birebir aynalar.
- **Rota koruması UX'tir, yetki backend'dedir:** `ProtectedRoute` oturumsuz kullanıcıyı `/login`'e (+`from` geri dönüş adresi), rol uyuşmazlığını `/403`'e gönderir; `GuestRoute` oturumluyu login/register'dan rol ana sayfasına yönlendirir (Customer → `/portal`, Admin/Personel → `/admin`). Gerçek yetki her zaman `[Authorize]` + kaynak sahipliğindedir; rol adları backend `UserRole` enum'u ile birebir aynıdır.
- **İş mantığı frontend'e sızmaz (CLAUDE.md §10):** Fiyatlama, durum geçişleri ve doğrulamanın son sözü backend'dedir; Zod yalnızca kullanıcı deneyimi için ön doğrulamadır.
- **Tasarım sistemi sahiplidir (ADR-027):** shadcn CLI/Radix bağımlılığı yoktur; dar bileşen seti (`Button/Input/Label/Select/Card/Badge/Spinner/Alert/FormField/EmptyState/Skeleton`) shadcn konvansiyonlarıyla elde yazılmıştır, ihtiyaç oldukça aynı desenle genişletilir.
- **Portal dashboard & pagination UX (ADR-045):** `PortalHomePage` yaşayan bir müşteri paneli — karşılama bandı (avatar + kişisel özet), Quick Actions, tıklanabilir durum kartları ve son teklif/poliçe akışları; tümü mevcut müşteri uçlarından (`totalCount` + son kayıtlar) kompoze edilir, yeni API yoktur. Sayfalama `shared/lib/pagination.ts`'te merkezidir: **Portal** varsayılanı 6 (+ ortak `Pagination`'da "Toplam N kayıt"), **Admin** 10/20/50/100 seçilebilir `PageSizeSelector` (segmented control) ile ve tercih `useAdminPageSize` → localStorage'da hatırlanır. Backend `pageSize` parametresi zaten vardı → değişiklik/ek yük yok.
- **Admin fiyatlandırma yönetimi (ADR-048):** Baz primler artık **effective-dated, değişmez tarife versiyonlarında** tutulur (`PricingVersion` + `PricingBranchRate`); fiyat değişikliği = yeni versiyon (güncelleme/silme yok → geçmiş ayrı audit tablosu olmadan doğal olarak saklanır). **Çekirdek garanti:** teklif oluşturulurken yürürlükteki versiyon `Quote.PricingVersionId`'de **sabitlenir**; teklif detayı/poliçe detayı/PDF dahil tüm yeniden hesaplar bu sabitlenen tarifeyi kullanır → tarife değişse bile geçmiş primler değişmez (ADR-021 determinizmi veri düzeyinde garantiye bağlanır). `IPricingEngine.CalculatePremium(request, rates?)` additive opsiyonel parametreyle saf kalır; `rates` yoksa yerleşik baseline (eski kayıtlar bit-aynı). `IPricingRateResolver` iki yolu ayırır: yeni fiyatlama → yürürlükteki tarife, mevcut teklif → sabitlenen tarife. Geçmişe tarihleme ve kısmi tarife yasaktır. Yetki **yalnızca Admin**. Kapsam bilinçli olarak baz primlerdir; aktüeryal bant çarpanları kural motoruna açılmaz.
- **Fiyatlandırma yönetimi ekranı — operasyonel yeniden tasarım (ADR-049):** `/admin/pricing` gerçek bir tarife operasyon ekranına dönüştü. **Yerleşik baz tarife artık görünür:** yeni `IPricingBaselineProvider` (Application arayüzü) + Infrastructure impl motorla **aynı** sabit primleri açar; `GetPricingVersions` bunu **v0 "Yerleşik Varsayılan Tarife"** olarak listeler (`PricingVersionDto`'ya additive `IsBaseline` alanı) → "Varsayılan" etiketi kalkar, form gerçek sayılarla ön dolar, fiyat frontend'de kopyalanmaz (tek kaynak backend). Yürürlükteki tarife tablo halindedir (güncel/önceki/değişim %/durum); yeni tarife yayınlama **karşılaştırmalı giriş + canlı özet + etki uyarısı + yayın öncesi onay drawer'ı** ile korunur; geçmiş satırları tıklanınca detay `Drawer`'ı açılır (yeni route yok). Çarpanlar kod-sabit olduğundan sahte "çarpan yönetimi" kurulmadı; "Fiyatlandırma Nasıl Çalışır?" paneli motorun gerçek modelini (Baz Prim × sabit risk çarpanları + branş→faktör listesi) salt-okunur anlatır. **Migration gerektirmez** (yalnızca okuma/sunum); motor, ADR-021 determinizmi, versiyon sabitleme ve değişmezlik aynen korunur.
- **Bonus-Malus: hasar geçmişinin tek ölçeği (ADR-059):** Önceden iki bağımsız çarpan vardı — yenilemede `ClaimHistoryFactor` (malus) ve hiç çalışmayan `NoClaimTier` (bonus). Aralarında değişmez olmadığından **çelişkili sonuç** üretebiliyorlardı (ör. 3 hasarlı + yüksek basamaklı müşteri ≈ nötr fiyat) ve malus hiç sönümlenmiyordu. Artık hasar geçmişi **tek basamakla** temsil edilir: `clamp(hasarsız dönem − 2×hasar, −3, +6)` → çarpan 1.60…0.70. **Durumsuz** hesaplanır, **branş bazlıdır** (Kasko ve Trafik ayrı), **yalnızca araç branşlarında** uygulanır (Sağlık/Konut/DASK tip düzeyinde izole) ve teklif anında `PricingSnapshot.NoClaimTier`'a **dondurulur**. Yeni müşteri 0. basamaktan başlar — dış geçmiş **varsayılmaz**. Basamak 0 iken döküm kalemi üretilmez → eski kayıtların dökümü birebir korunur. `Quote.ClaimHistoryFactor` **silinmedi**: ADR-059 öncesi tekliflerin primi/dökümü korunsun diye saklanır ve uygulanmaya devam eder; yeni tekliflerde 1.00'dır (setter kaldırıldı). **Migration gerekmedi** — `NoClaimTier` kolonu zaten vardı ve 0 her iki ölçekte de nötrdür.
- **Tek fiyatlama girdi yolu + deprem bölgesi semantiği (ADR-056/058):** Teklif oluşturma, karşılaştırma önizlemesi **ve yenileme** artık girdiyi tek bir yerden (`IQuotePricingInputBuilder`) kurar → ikinci bir hesaplama yolu yoktur, parite yapısaldır. `Property.EarthquakeZone` **yalnızca sistem tarafından adresin ilinden türetilir**; bölgeyi değiştiren domain metodu/API ucu yoktur (ölü `Property.UpdateDetails` kaldırıldı). ADR-055 öncesi kayıtlardaki değer müşteri beyanıdır ve **tarihsel doğruluk için korunur** — geriye dönük düzeltilmez, çünkü snapshot'ı olmayan eski tekliflerin yeniden hesabı bu alanı okur ve değiştirilmesi onların prim dökümünü bozardı. "Bilinmeyen bölge" sentinel'i tek sabitte (`EarthquakeZoneDefaults.Unknown`) toplandı.
- **Fiyatlama girdisi snapshot'ı + gerçek veriye dayalı faktörler (ADR-053/054/055):** ADR-021 primi saklar, ADR-048 tarifeyi sabitler; eksik halka olan **girdiler** artık `Quote.PricingSnapshot` (nullable owned) ile teklif oluşturulurken dondurulur ve tüm yeniden hesaplar (detay/poliçe/PDF) yalnızca ondan okur → müşteri ilini veya aracını değiştirse bile **eski teklifin primi ve dökümü değişmez** (`null` snapshot = eski kayıt → bit-aynı davranış). Snapshot'a yalnızca fiyatı belirleyen primitifler girer; kişisel veri taşınmaz. **Ölü faktörler kaldırıldı:** hasarsızlık basamağı türetilemediği ve yenilemedeki `ClaimHistoryFactor` ile **aynı riski iki kez fiyatlayacağı** için dökümden çıkarıldı; **sigara beyanı** artık Sağlıkta zorunlu olarak toplanır (varsayılansız) ve beyan yoksa faktör gösterilmez. Hasar geçmişi **branş kapsamlı** sayılır (Kasko hasarı Sağlık yenilemesini etkilemez). **Deprem bölgesi** kullanıcı seçiminden çıkarılıp konutun ilinden türetilir (gömülü JSON, Türkçe kültüre duyarlı eşleşme; il düzeyinde MVP yaklaşıklaması olduğu dokümante edilmiştir). `PricingEngine` **değişmedi**.
- **Operasyon dashboard'u (ADR-052):** `/admin` artık "kaç kayıt var" değil, **dönemsel karar paneli**. `GET /dashboard/summary?from=&to=` tüm blokları **tek çağrıda** döner (varsayılan son 30 gün); karşılaştırma **eşit uzunluktaki, hemen önceki, örtüşmeyen** dönemledir. **Payda 0 iken oran/değişim `null`** → yanıltıcı "%0"/"+%100" üretilmez. Seri kova genişliği aralıktan türer (saat/gün/ay) ve **`Policy.CreatedAt`** (üretim tarihi) bazlıdır — poliçe listesindeki `StartDate`'ten bilinçli olarak farklıdır, grafikte belirtilir. Satış hunisi gerçek `QuoteStatus` yaşam döngüsünden gelir (Draft kalıcı değil → "Fiyatlandı"dan başlar; "Onaylanan" satın alınanları içerir → monoton azalır); branş performansı **tek kohort/tek sorgudan** olduğundan dönüşüm %100'ü aşamaz. **Aksiyon Merkezi** (bekleyen teklif/hasar, yaklaşan yenileme, başarısız ödeme) ilgili admin ekranına götürür; sayaç birden çok durumu kapsadığından bağlantı filtresiz açılır (uyuşmayan filtreyle yanıltılmaz). **Son Aktiviteler** mevcut bildirim altyapısından beslenir (ADR-047) — yeni endpoint/audit sistemi yok. Kâr/komisyon/"aktif müşteri"/"bekleyen ödeme" gibi veri modelinin desteklemediği metrikler **eklenmedi**. Tüm hesaplar SQL tarafı `COUNT/SUM/GROUP BY` (N+1 yok); **migration gerekmez**.
- **Admin müşteri kimliklendirme + format bağımsız telefonla arama (ADR-051):** Admin teklif/poliçe ekranlarında müşteri kimliği **Ad Soyad + Telefon + stabil `CustomerId`** ile netleştirildi (aynı isimli müşteriler telefonla ayırt edilir). DTO'lara additive `CustomerFullName`/`CustomerPhone` (+`CustomerId`) eklendi; snapshot yok — canlı `CustomerId` bağıyla okunur. Telefon kanonik saklanır (`+90…`); yeni `PhoneNumberSearch` girdiyi abone son ekine indirger ve `PhoneNumber.Replace("+","").Contains(...)` (EF→SQL) ile eşler → `05551111111`/`0555 111 11 11`/`+90 555…` aynı müşteriyi bulur (**migration yok**). Arama Customer'ı zaten Include eden tek sorguya eklendi (**N+1 yok**); poliçe araması ayrıca poliçe numarasını kapsar. Sağlıkta "Sigorta Ettiren (Müşteri)" vs "Sigortalı" ayrımı korundu ve poliçe detayında düzeltildi (`InsuredPerson` artık taşınıyor). KVKK: TCKN/tam adres listede yok; telefon operasyonel ayırt edici; müşteri kapsamı sızdırmaz.
- **Hasarda saat hassasiyetli teminat penceresi (ADR-050):** Poliçe satın alma anında (saat dahil, UTC) aktifleşir; aynı gün başlangıç saatinden **sonraki** hasar geçerli, **önceki** geçersizdir. Kural `Policy.CoversIncidentAt(incidentDate) => StartDate ≤ incidentDate ≤ EndDate` (sınırlar dahil) predikatıyla domain'de; handler bunu çağırır (`BusinessRuleException`/409 aynı). Frontend olay **tarih + saat**ini birleştirip UTC ISO gönderir (API alanı `incidentDate` aynı, artık saat taşır) — eskiden yalnızca tarih (gece yarısı) gönderildiğinden aynı gün hasarı yanlışlıkla reddediliyordu. `Policy.StartDate/EndDate` zaten saat taşıdığından **yeni alan/migration yoktur**.
- **Operasyonel bildirim detaylandırma (ADR-047):** Bildirim kataloğu artık bağlam üretir — "kim yaptı / kimin için / ne / hangi kayıt / ne kadar". `Notification`'a additive `ActorUserId` + `ActorName` (olay anındaki **snapshot**; kullanıcı adı değişse de geçmiş bildirim değişmez) + `ReferenceCode` (gerçek `PolicyNumber`; teklif/hasar numarası veri modelinde yoktur, uydurulmaz) eklendi → additive migration `EnrichNotificationContext`. Bağlam `INotificationContextResolver` ile mevcut `ICurrentUserService`/`ICustomerRepository` üzerinden çözülür (personel için sorgu yok); **audit log sistemi kurulmaz**. Navigasyon: admin detayları Drawer olduğundan derin bağlantı `?focus=<id>` ile kurulur (`useFocusedRecord`), yeni route açılmaz; hedefi olmayan sistem bildirimlerinde bağlantı gösterilmez. Toast kısa (başlık + referans), detay Bildirim Merkezi'ndedir. KVKK: TCKN/telefon/kart/sağlık detayı taşınmaz; şifre sıfırlama bildirimi e-posta içermez.
- **PO sadeleştirme (ADR-046):** Bildirim sistemi tasarım gereği **yalnızca staff**'a çalışır (SignalR yalnız Admin/Personel'de bağlanır; zil AdminLayout'ta; Bildirim Merkezi `/admin/notifications`). Müşteri hiç bildirim almadığından müşteri profilindeki "Bildirimler" tercih sekmesi kaldırıldı (yanlış beklenti kuruyordu); `NotificationPreferencesPanel` admin Bildirim Merkezi'nde kullanılmaya devam eder. Müşteri teklif listesinden karar üretmeyen `createdAt` çıkarıldı (geçerlilik/aciliyet zaten gösteriliyor); admin teklif tablosundaki "Tarih" korunur. İş kuralı/API/DTO/CQRS değişmedi.
- **Premium UI/UX modernizasyonu (ADR-044):** `Card` primitifi merkezi elevation sistemi taşır (`rounded-xl` + yumuşak gölge + `transition` → tüketici `hover:*` sınıfları her yerde pürüzsüz) — tek dosyadan tüm kartlar SaaS seviyesine çıkar. Paylaşılan `EmptyState` (ikon + başlık + açıklama + CTA) ve `Skeleton`/`SkeletonRows` primitifleri düz "kayıt yok" metinlerinin ve çıplak spinner'ların yerini alır; liste ekranları (müşteri + admin), bildirim merkezi ve dashboard bunları kullanır. KPI `StatCard`'ı additive `icon`/`footer` alır; "Toplam Prim Üretimi" gerçek 12 aylık seriden beslenen bağımlılıksız SVG `Sparkline` gösterir. Aylık trend grafiği gradyanlı **area**'dır ve açıklaması metriği dürüst tanımlar (brüt üretilen prim — tüm poliçe durumları dahil; backend/CQRS değişmeden). Tümü token tabanlı → Dark Mode uyumlu. Kullanıcı menüsü gibi feature bileşenleri layout'lara **slot** (`userMenu` prop'u) olarak routes.tsx'ten enjekte edilir — shared katmanı feature'ları bilmez (ADR-029).
- **Dark Mode / merkezi tema (ADR-043):** `shared/theme/useTheme.tsx` (`ThemeProvider` + `useTheme`) `light | dark | system` modunu yönetir; `.dark` sınıfını `<html>`'e uygular, `localStorage["sigortapro.theme"]`'de saklar, system modunda `prefers-color-scheme`'i canlı izler. FOUC, `index.html`'deki inline script (aynı anahtar/mantık) ile önlenir. `shared/theme/ThemeToggle.tsx` üç navbar'da (Landing/Customer/Admin) Güneş/Ay/Monitör ikonuyla döngüsel geçiş sunar (Framer Motion — küçük animasyon). Renk kimliği token'lardan gelir (globals.css `.dark` — ADR-040 paleti); yeni token/hardcoded renk yoktur. **PDF ve mail kapsam dışıdır** (her zaman kurumsal açık tema).

### Müşteri Portalı — Profil & Teklif Sihirbazı (Task 17)

Portalın vitrin deneyimi iki feature ile kurulur: `features/profile` (profil + araç/konut yönetimi) ve `features/quotes` (teklif sihirbazı, tekliflerim listesi/detayı). Öne çıkan kararlar (ADR-030):

- **Sayısal enum mirror'ları:** Backend `JsonStringEnumConverter` kaydetmediğinden Domain enum'ları JSON'da **sayısal** döner (canlı API ile doğrulandı). `shared/types/insurance.types.ts` bunları `as const` sayısal nesne/tip olarak yansıtır (+ Türkçe etiket/rozet haritaları) ve API'ye sayısal gönderir — `shared/types/auth.types.ts`'teki `UserRole` mirror desenini izler. Para/tarih/gün-sayacı biçimlendirme `shared/utils/format.ts`'te (native `Intl`, ek paket yok).
- **Çok adımlı sihirbaz (`QuoteWizardPage`):** branş → risk bilgileri → anlık prim/risk skoru + paket karşılaştırma. Backend `CreateQuoteCommand` yalnızca kalıcı `VehicleId`/`PropertyId` aldığından, risk adımı mevcut araç/konuttan seçtirir veya **aynı ekranda** ekletir; ekleme, profil feature'ının `VehicleForm`/`PropertyForm` + `useAddVehicle`/`useAddProperty`'sini **yeniden kullanır** (DRY). Anlık prim + risk skoru + 3 paket tek `GET /quotes/compare` çağrısından beslenir. Sağlık branşı risk objesi gerektirmez.
- **Cross-feature yön:** `features/quotes` → `features/profile` (sihirbaz profil risk objelerini okur/ekler) tek yönlü ve döngüsüzdür; ortak parçalar `shared`'dadır.
- **Tekliflerim/detay:** durum rozetleri + geçerlilik sayacı + durum filtresi/sayfalama (`GET /quotes`); detayda prim dökümü/teminatlar + onayla/reddet (`POST /quotes/{id}/approve|reject`). Onaylanınca detay, ödeme sayfasına yönlendiren aktif "Satın Al" gösterir (Task 18).

### Müşteri Portalı — Satın Alma & Poliçe Ekranları (Task 18)

Teklif → ödeme → poliçe → PDF müşteri akışı iki feature ile tamamlanır: `features/payments` (mock ödeme + başarı ekranı) ve `features/policies` (Poliçelerim liste/detay + PDF). Öne çıkan kararlar (ADR-031):

- **Ödeme sayfası (`PurchasePage` — `/portal/quotes/:id/purchase`):** yalnızca `Approved` teklif için taksit seçenekleri (`GET /payments/installment-options`) + kart formu (RHF + Zod, backend `PurchaseQuoteCommandValidator` aynası) + test kartı ipuçları gösterir. Tek sayfalı durum makinesi: başarılı ödemede (`POST /payments`) aynı sayfa `PurchaseSuccess` (poliçe künyesi + PDF indirme + yönlendirmeler) render eder — sonuç route'lar arası taşınmaz (KISS). **Başarısız senaryo** (402) `getApiErrorMessages` ile RFC 7807 `detail`'inden gösterilir; teklif/poliçe değişmez.
- **Poliçelerim (`PolicyListPage` — `/portal/policies`):** durum sekmeleri (Aktif / Süresi Dolmuş / İptal / Tümü; her biri tek `GET /policies?status=` filtresi) + sayfalama + durum rozetleri. **Detay (`PolicyDetailPage`):** künye + risk objesi + **teminat tablosu** (`GET /policies/{id}`; teklifle aynı `CoverageList`, ADR-021 deterministik) + PDF indirme.
- **PDF blob indirme:** Bearer başlığı axios interceptor'ında eklendiğinden `<a href>` yerine `responseType:"blob"` ile indirilir; dosya adı `Content-Disposition`'dan çıkarılır (`filename` + `filename*=UTF-8''`), `URL.createObjectURL` + geçici `<a download>` ile kaydettirilir. İndirme `useMutation` ile modellenir.
- **İş mantığı backend'de:** Luhn/senaryo bazlı ret, poliçeleştirme, teminat hesabı backend'dedir; Zod yalnızca yapısal kart doğrulamasıdır. Numaralı enum sözleşmesi (ADR-030) `PolicyStatus`/`PaymentStatus`'e genişletildi.

### Müşteri Portalı — Hasar & Yenileme (Task 19)

Müşteri tarafı süreçleri iki feature ile tamamlanır: `features/claims` (hasar bildirimi + takip) ve `features/renewals` (yenileme teklifi + onay). **Backend'e dokunulmadı** — Task 12/13 uçları (`ClaimsController`/`RenewalsController`) olduğu gibi tüketildi. Öne çıkan kararlar (ADR-032):

- **Hasar (`features/claims`):** `ClaimCreatePage`/`ClaimForm` (`/portal/claims/new`) — poliçe seçici **yalnızca aktif poliçeleri** Task 18 `GET /policies?status=Active`'ten alır (DRY, `claims → policies`); olay tarihi/tutar/açıklama (RHF + Zod, `CreateClaimCommandValidator` aynası) + **mock foto** (dosya adları metadata olarak; yüklenmez — ADR-024). `ClaimListPage` durum filtresi + sayfalama; `ClaimDetailPage` künye + değerlendirme notu + **durum zaman çizelgesi** (`ClaimTimeline`: Bildirildi → İncelemede → Onaylandı → Ödendi | Reddedildi çatalı). Durum-geçişi (inceleme/onay/ret/ödeme) `Staff` işidir → Task 20.
- **Yenileme (`features/renewals`):** `RenewalListPage`/`RenewalCard` (`/portal/renewals`) — `GET /renewals` ile bekleyen teklifler (prim + geçerlilik sayacı, `renewals → quotes`); **onay** (`POST /renewals/{id}/accept`) yeni dönem teklifini Approved'a çeker ve **"Ödemeye Geç"** ile mevcut Task 18 `PurchasePage`'e köprülenir (DRY — Task 13 backend'inin `PurchaseQuoteCommand`'ı yeniden kullanması ile simetrik).
- **Altyapı:** `ClaimStatus` sayısal enum mirror'ı (ADR-030 deseni) + tasarım sistemine `Textarea` (ADR-027). Cross-feature bağımlılıklar tek yönlü ve döngüsüzdür; iş mantığı ve durum geçişleri backend'de kalır (CLAUDE.md §10).

### Acente Admin Paneli (Task 20)

Acente personelinin (Admin/Personel) çalışma yüzeyi `/admin` altındadır (`AdminLayout` — sidebar nav: Dashboard/Müşteriler/Teklifler/Poliçeler/Hasarlar). **Backend'e dokunulmadı** — Task 14 dashboard uçları ve modüllerin `Staff` uçları olduğu gibi tüketildi. Öne çıkan kararlar (ADR-033):

- **Dashboard (`features/dashboard` — `/admin`):** 6 KPI kartı (toplam prim, aktif poliçe, bekleyen teklif/hasar, yenileme oranı, hasar/prim oranı) + **aylık prim trendi** (tek serili sütun) + **branş dağılımı** (tek hue yatay bar — büyüklük karşılaştırması; kimlik eksen etiketinde, poliçe adedi tooltip'te; ikinci eksen yok) + **en riskli müşteriler** (ilk 5). Grafikler **Recharts** ile tema token'larından (`hsl(var(--primary))`) çizilir — koyu tema otomatik; Recharts lazy `AdminDashboardPage` chunk'ına hapsedilir (ana bundle etkilenmez).
- **Yönetim ekranları — tablo + filtre + detay çekmecesi:** El yazımı `Drawer` (shared; overlay/Escape, `role="dialog"` — ADR-027 konvansiyonu, Radix yok) dört ekranda ortak. **Müşteriler** (`features/customers`, yeni): debounce'lu ad/soyad/TCKN araması + il filtresi (`GET /customers`), çekmecede profil + araç/konut (`GET /customers/{id}` — backend `CustomerDto` profil ekranıyla aynı olduğundan `CustomerProfile` tipi yeniden kullanılır). **Teklifler** (`features/quotes`): durum+branş filtresi (personel tümünü görür — Task 9), çekmecede prim dökümü/teminatlar mevcut bileşenlerle. **Poliçeler** (`features/policies`): personelin tüm poliçeleri listeleyebildiği tek uç olan `GET /dashboard/reports/policies` tarih aralığı filtresiyle (`policies → dashboard` tek yönlü); çekmecede personel-muaf `GET /policies/{id}` + PDF indirme. **Hasarlar** (`features/claims`): durum filtresi, çekmecede açıklama/not + `ClaimTimeline` + karar aksiyonları.
- **Hasar karar aksiyonları (`ClaimDecisionPanel`):** Durum makinesini aynalar — Submitted → İncelemeye Al; UnderReview → Onayla (tutar + not; `ApproveClaimCommandValidator` aynası Zod) / Reddet (gerekçe zorunlu); Approved → Ödemeyi Gerçekleştir. Geçersiz geçişin son sözü backend'de 409'dur (ADR-013); mutation'lar hasar liste+detay cache'ini geçersizleştirir.
- **Altyapı:** `Pagination` + `Drawer` (shared/components), `useDebounce` (shared/hooks, DEVELOPMENT_RULES §6), `formatPercent`/`formatCompactCurrency`/`formatMonthLabel` (shared/utils). Yeni npm bağımlılığı yalnızca `recharts`.

## Test Altyapısı — FAZ 3 (Task 21)

Test projeleri (`tests/`) katmanlarla bire bir eşleşir (DEVELOPMENT_RULES.md §5.1); xUnit + FluentAssertions + NSubstitute kullanılır. Task 21 ile paket iki seviyede tamamlanmıştır:

- **Birim testler** (Task 3–20 boyunca biriken): fiyatlama motoru kural senaryoları (`Infrastructure.Tests/Services/Pricing`), domain durum makineleri (`Domain.Tests` — Quote/Policy/Claim/Payment/Renewal geçişleri), handler/validator/pipeline testleri (`Application.Tests`), middleware testleri (`WebAPI.Tests/Middleware`).
- **Entegrasyon testleri** (`WebAPI.Tests/Integration`, ADR-034): `WebApplicationFactory<Program>` gerçek HTTP pipeline'ını (middleware → authentication → MediatR → EF Core → Identity) **SQLite in-memory** veritabanıyla ayağa kaldırır. Kapsam: auth akışı (`register` → `login` → refresh **rotasyonu** + 401/409/400 negatifleri) ve teklif→satın alma akışı (araç ekle → teklif oluştur → onayla → mock POS ödeme → **aktif poliçe** + `Purchased` teklif; 402 yetersiz bakiye → teklif `Approved` kalır; 409 onaysız satın alma; 403 sahiplik ihlali).

**Öne çıkan kararlar (ADR-034):**

- **SQLite in-memory + `EnsureCreated`:** Şema EF modelinden üretilir (SQL Server'a özgü migration'lar çalıştırılmaz); tek açık `SqliteConnection` factory ömrü boyunca yaşar. TestContainers/gerçek SQL Server bilinçli elenmiştir (kurulum bağımlılığı olmadan her ortamda `dotnet test`).
- **"Testing" ortamı:** `Program.cs`'in Development'a özel migrate/seed bloğu ve Swagger devre dışıdır; şema + referans verisi (5 ürün + roller, prodüksiyondaki `DbSeeder`/`IdentitySeeder` ile) factory başlatılırken yüklenir. Testler kendi kullanıcı/araç/teklif verisini kendileri oluşturur (§5.4).
- **Tek paylaşılan factory (collection fixture):** Serilog bootstrap logger'ı ilk host kurulumunda dondurulduğundan aynı test sürecinde ikinci bir host kurulamaz; tüm entegrasyon sınıfları `IntegrationTestCollection` üzerinden tek host'u paylaşır ve sıralı çalışır (SQLite bağlantısının eşzamanlı kullanımı da böylece önlenir).
- **Rate limit bütçesi:** Auth uçları IP başına 10 istek/dk limitlidir (ADR-020); testlerin arrange aşaması HTTP yerine `ISender` (MediatR) üzerinden kayıt yapar (`TestAccountFactory`), HTTP auth çağrıları yalnızca fiilen test edilen uçlara harcanır.
- **Arkaplan servisi devre dışı:** `PolicyLifecycleBackgroundService` test host'undan çıkarılır (komutları Task 13 birim testlerinde kapsanır; paylaşılan SQLite bağlantısına eşzamanlı erişim testleri kırılgan yapardı).

## Mimari Kararlar

Tüm önemli mimari kararlar ve gerekçeleri `docs/ai/DECISIONS.md` dosyasında (ADR-001…034; yerel geliştirme dokümanı — `.gitignore` ile repo dışında) kayıtlıdır. Bu doküman kuruluşta ADR-001 (Clean Architecture katman yapısı) kararını uygular; Domain modeli ADR-013 ve ADR-014 kararlarını, Application/CQRS omurgası ADR-002, ADR-013 ve ADR-015 kararlarını, Persistence katmanı ADR-005, ADR-010 ve ADR-016 kararlarını, kimlik doğrulama ADR-003, ADR-014 ve ADR-017 kararlarını, cross-cutting API altyapısı ADR-018 (exception handling/ProblemDetails), ADR-019 (API versiyonlama) ve ADR-020 (rate limiting + güvenlik header'ları) kararlarını uygular (ADR-012, ADR-014 ile revize edilmiştir).

## Durum

Bu doküman **Task 1 — Solution İskeleti**, **Task 2 — Domain Katmanı**, **Task 3 — Application Katmanı Altyapısı (CQRS Çekirdeği)**, **Task 4 — Persistence Katmanı (EF Core + SQL Server)**, **Task 5 — Kimlik Doğrulama & Yetkilendirme (JWT + Roller)** ve **Task 6 — API Çapraz Kesit Altyapısı (Cross-Cutting)** kapsamında oluşturulmuş; FAZ 1 iş modülleri **Task 7 — Müşteri & Profil Modülü**, **Task 8 — Risk Analizi & Dinamik Fiyatlama Motoru (Mock)**, **Task 9 — Teklif (Quote) Modülü**, **Task 10 — Ödeme (Mock Sanal POS) & Poliçeleştirme**, **Task 11 — PDF Poliçe Dökümanı Üretimi**, **Task 12 — Hasar (Claim) Modülü**, **Task 13 — Poliçe Yenileme (Renewal) & Arkaplan İşleri** ve **Task 14 — Admin Dashboard & Raporlama API'si** ile tamamlanmıştır. FAZ 2'den **Task 15 — React Proje Kurulumu & Tasarım Sistemi** (`frontend/` SPA iskeleti), **Task 16 — Auth Ekranları & Oturum Yönetimi** (kayıt/giriş/çıkış, AuthProvider, role göre yönlendirme, guard'lar + 401/403 sayfaları), **Task 17 — Müşteri Portalı: Profil & Teklif Sihirbazı** (profil + araç/konut yönetimi, çok adımlı teklif sihirbazı — anlık prim/risk skoru + paket karşılaştırma, tekliflerim listesi/detayı + onayla/reddet), **Task 18 — Satın Alma & Poliçe Ekranları** (mock ödeme sayfası — taksit + test kartı senaryoları, satın alma sonrası başarı ekranı, Poliçelerim liste/detay + teminat tablosu + PDF indirme; **backend'e ADR-031 ile iki salt okunur additive poliçe ucu eklendi**), **Task 19 — Müşteri Portalı: Hasar & Yenileme** (hasar bildirim formu + durum zaman çizelgeli takip, yenileme teklifi kartı + onay→ödeme köprüsü; ADR-032, **backend'e dokunulmadı**) ve **Task 20 — Admin Paneli** (dashboard: KPI kartları + Recharts grafikleri — aylık prim trendi, branş dağılımı — + en riskli müşteriler; yönetim ekranları: müşteriler/teklifler/poliçeler/hasarlar tablo + filtre + detay çekmecesi; hasar karar aksiyonları; ADR-033, **backend'e dokunulmadı**; tümü canlı backend'e karşı uçtan uca doğrulandı) tamamlanmıştır. FAZ 2 (frontend) tamamdır. FAZ 3'ten **Task 21 — Test Altyapısı** (WebApplicationFactory + SQLite in-memory entegrasyon testleri — auth ve teklif→satın alma akışları; ADR-034; toplam 263 test yeşil) ve **Task 22 — Dokümantasyon Finali & Çalıştırma Deneyimi** ([`README.md`](README.md) finali, [`API.md`](API.md) uç nokta özeti, bu dokümanın son gözden geçirmesi) tamamlanmıştır. **MVP teslime hazırdır**; MVP dışı bırakılan entegrasyonlar (gerçek POS, e-posta, foto depolama, blob storage) ve gelecek faz adayları `docs/ai/DECISIONS.md > Bekleyen / Gelecek Kararlar` tablosunda izlenir.
