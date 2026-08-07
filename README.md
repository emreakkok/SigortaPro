# SigortaPro

Tek acenteli, B2C sigorta poliçe yönetim sistemi. Müşteriler self-servis olarak teklif alır, teminat paketlerini karşılaştırır, poliçe satın alır, poliçe sertifikası PDF'ini indirir, hasar bildirir ve yenileme tekliflerini onaylar. Acente personeli ise dashboard, raporlama, müşteri/teklif/poliçe yönetimi ve hasar karar süreçlerini yönetir.

## İçindekiler

- [Proje Tanımı](#proje-tanımı)
- [Kullanılan Teknolojiler](#kullanılan-teknolojiler)
- [Mimari](#mimari)
- [Kurulum](#kurulum)
- [Çalıştırma](#çalıştırma)
- [Testler](#testler)
- [Ekran Görüntüleri](#ekran-görüntüleri)
- [Lisans](#lisans)

## Proje Tanımı

SigortaPro iki ana kullanıcı yüzeyi sunar:

- **Müşteri Portalı** — Kayıt/giriş, profil ve risk objesi (araç/konut) yönetimi, çok adımlı teklif sihirbazı (anlık prim + risk skoru + paket karşılaştırma), ödeme ve poliçeleştirme, "Poliçelerim" ve poliçe PDF indirme, hasar bildirimi ve takibi, yenileme tekliflerinin onayı.
- **Acente Paneli** — Operasyon dashboard'u (KPI'lar, grafikler, raporlar), müşteri/teklif/poliçe/hasar yönetimi, iki taraflı hasar karar akışı, versiyonlanmış fiyatlandırma yönetimi ve personel yönetimi.

Desteklenen branşlar: **Kasko, Trafik, Konut, DASK, Sağlık.**

## Kullanılan Teknolojiler

**Backend**

- ASP.NET Core Web API (.NET 8)
- Entity Framework Core (Code-First) + SQL Server
- MediatR (CQRS)
- ASP.NET Core Identity + JWT kimlik doğrulama
- FluentValidation, Serilog, QuestPDF (poliçe PDF), SignalR (gerçek zamanlı bildirim)

**Frontend**

- React 18 + TypeScript + Vite
- TanStack Query, React Hook Form + Zod
- Tailwind CSS

Ayrıntılı frontend kurulumu ve klasör yapısı için [`frontend/README.md`](frontend/README.md).

## Mimari

Proje Clean Architecture katmanlarıyla düzenlenmiştir; bağımlılıklar içten dışa doğru akar (Domain hiçbir katmana bağımlı değildir).

```
SigortaPro/
├── src/
│   ├── Core/
│   │   ├── SigortaPro.Domain/          # Entity, enum, domain kuralları (sıfır bağımlılık)
│   │   └── SigortaPro.Application/     # CQRS, DTO, arayüzler, doğrulama
│   ├── Infrastructure/
│   │   ├── SigortaPro.Persistence/     # EF Core, DbContext, repository, migration
│   │   └── SigortaPro.Infrastructure/  # PDF, e-posta, ödeme, JWT token servisleri
│   └── Presentation/
│       └── SigortaPro.WebAPI/          # Controller, middleware, DI kompozisyonu
├── frontend/                           # React SPA (müşteri portalı + acente paneli)
├── tests/                              # Birim + entegrasyon testleri
├── API.md                              # API uçlarının özeti
├── ARCHITECTURE.md                     # Katman yapısı ve modül detayları
├── PRICING.md                          # Fiyatlama motoru kuralları
└── SigortaPro.sln
```

Katman detayları için [`ARCHITECTURE.md`](ARCHITECTURE.md), tüm API uçlarının özeti için [`API.md`](API.md), fiyatlama kuralları için [`PRICING.md`](PRICING.md).

## Kurulum

### Gereksinimler

- .NET 8 SDK
- SQL Server (geliştirme için SQL Server Express yeterlidir)
- Node.js 20.19+ veya 22.12+ (frontend için)

### Bağımlılıklar

```bash
# Backend bağımlılıkları
dotnet restore

# Frontend bağımlılıkları
cd frontend
npm install
cd ..
```

## Çalıştırma

### Backend (API)

```bash
# Derle
dotnet build

# Veritabanı migration'larını uygula
dotnet ef database update --project src/Infrastructure/SigortaPro.Persistence

# API'yi çalıştır (geliştirme ortamında başlangıçta migrate + seed otomatik çalışır)
dotnet run --project src/Presentation/SigortaPro.WebAPI
```

- API varsayılan olarak `http://localhost:5153` adresinde çalışır.
- Geliştirme ortamında etkileşimli API dokümantasyonu: `http://localhost:5153/swagger`
- Sağlık kontrolü: `GET /health`

Bağlantı dizesi `src/Presentation/SigortaPro.WebAPI/appsettings.Development.json` içinde yerel SQL Server Express instance'ını (`.\SQLEXPRESS`) hedefler; kendi ortamınıza göre düzenleyebilirsiniz. JWT imzalama anahtarı geliştirmede bir placeholder'dır; yerel çalıştırmadan önce en az 32 karakterlik bir değerle (örneğin `dotnet user-secrets` ile) sağlayın. Üretimde imzalama anahtarı ve bağlantı dizesi ortam değişkeni / secret store ile verilmelidir.

### Frontend (React SPA)

```bash
cd frontend
cp .env.example .env    # varsayılan: VITE_API_BASE_URL=http://localhost:5153/api/v1
npm run dev             # http://localhost:5173
npm run build           # prodüksiyon derlemesi (dist/)
```

### Geliştirme Seed Kullanıcıları

İlk çalıştırmada aşağıdaki örnek hesaplar oluşturulur (yalnızca geliştirme; üretimde kullanılmamalıdır):

| Rol | E-posta | Şifre |
|-----|---------|-------|
| Admin | `admin@sigortapro.com` | `Admin!2345` |
| Personel | `personel@sigortapro.com` | `Personel!2345` |
| Müşteri | `musteri@sigortapro.com` | `Musteri!2345` |

### Test Ödeme Kartları

Ödeme akışı, geliştirme ortamında gerçek POS yerine bir simülasyon kullanır. Kart numarası Luhn ile doğrulanır ve yalnızca son 4 hane saklanır.

| Kart Numarası | Sonuç |
|---------------|-------|
| `4111 1111 1111 1111` | Başarılı ödeme → poliçe oluşur |
| `4000 0000 0000 0002` | Başarısız — yetersiz bakiye |
| `4000 0000 0000 0069` | Başarısız — 3D Secure doğrulaması başarısız |

İzin verilen taksit sayıları: 1, 3, 6, 9, 12.

## Testler

```bash
dotnet test        # tüm birim ve entegrasyon testleri
```

- **Birim testleri** — fiyatlama motoru kuralları, domain durum makineleri (teklif/poliçe/hasar/ödeme/yenileme), handler ve doğrulama testleri (xUnit + FluentAssertions + NSubstitute).
- **Entegrasyon testleri** — gerçek HTTP pipeline'ı (middleware → JWT → MediatR → EF Core → Identity) **SQLite in-memory** ile test edilir; SQL Server veya Docker kurulumu gerekmez.

## Ekran Görüntüleri

> Ekran görüntüleri `docs/screenshots/` klasörüne eklendiğinde burada görünecektir.

| Müşteri Portalı | Acente Paneli |
|-----------------|---------------|
| Teklif sihirbazı | Dashboard |
| Ödeme ekranı | Hasar yönetimi |
| Poliçelerim | Fiyatlandırma yönetimi |
