# SigortaPro — Mimari Doküman

> Bu doküman projenin üst düzey mimarisini açıklar. Detaylı kurallar için [`docs/ai/ARCHITECTURE_RULES.md`](docs/ai/ARCHITECTURE_RULES.md), kararların gerekçeleri için [`docs/ai/DECISIONS.md`](docs/ai/DECISIONS.md) referans alınmalıdır.

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

### Application (`SigortaPro.Application`)
CQRS handler'ları, DTO'lar, FluentValidation validator'ları, repository/servis arayüzleri ve MediatR pipeline behavior'larını barındırır. Klasör iskeleti: `Common/{Behaviors,Exceptions,Interfaces,Models}/`, `Features/{Modül}/{Commands,Queries,DTOs}/`.

### Persistence (`SigortaPro.Persistence`)
EF Core `AppDbContext`, entity konfigürasyonları, generic repository implementasyonları, migration'lar ve seed verisi. Klasör iskeleti: `Context/`, `Configurations/`, `Repositories/`, `Migrations/`, `Seed/`, `Interceptors/`.

### Infrastructure (`SigortaPro.Infrastructure`)
Domain dışı teknik servisler: JWT token üretimi, mock fiyatlama motoru, mock sanal POS, QuestPDF ile poliçe dokümanı üretimi, mock bildirim servisi, arkaplan işleri. Klasör iskeleti: `Services/`, `BackgroundJobs/`.

### WebAPI (`SigortaPro.WebAPI`)
Controller'lar, middleware, filter'lar ve DI composition root. Klasör iskeleti: `Controllers/v1/`, `Middleware/`, `Filters/`, `Extensions/`.

## Mimari Kararlar

Tüm önemli mimari kararlar ve gerekçeleri için bkz. [`docs/ai/DECISIONS.md`](docs/ai/DECISIONS.md). Bu doküman kuruluşta ADR-001 (Clean Architecture katman yapısı) kararını uygular.

## Durum

Bu doküman **Task 1 — Solution İskeleti** kapsamında oluşturulmuştur. Entity ilişki diyagramı, CQRS omurgası, veritabanı şeması ve cross-cutting altyapı detayları ilerleyen task'larda bu dokümana eklenecektir.
