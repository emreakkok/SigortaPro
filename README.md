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
- SQL Server (LocalDB veya tam sürüm) — Task 4'te eklenecek
- Node.js 18+ — frontend için, FAZ 2'de eklenecek

## Kurulum ve Çalıştırma

> Bu bölüm proje ilerledikçe (migration, seed, çalıştırma adımları) güncellenecektir.

```bash
# Solution'ı derle
dotnet build

# Testleri çalıştır
dotnet test
```

## Durum

Proje şu anda **FAZ 0 — Temel & İskelet** aşamasındadır. Güncel görev listesi için bkz. [`docs/ai/TASKS.md`](docs/ai/TASKS.md).
