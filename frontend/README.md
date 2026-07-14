# SigortaPro — Frontend (React SPA)

Tek acenteli sigorta poliçe yönetim sisteminin müşteri portalı + admin paneli.
Backend API'si için bkz. kök dizindeki [`README.md`](../README.md).

## Stack

| Katman | Teknoloji |
|--------|-----------|
| UI framework | React 18 + TypeScript (strict, `any` yasak) |
| Build | Vite 8 |
| Routing | React Router v6 (`createBrowserRouter`, route bazlı lazy loading) |
| Server state | TanStack Query v5 |
| HTTP | Axios (JWT interceptor + otomatik refresh-token yenileme) |
| Form | React Hook Form + Zod (`@hookform/resolvers`) |
| Styling | Tailwind CSS v3 + shadcn/ui tarzı el yazımı bileşen seti (cva + tailwind-merge) |
| Grafikler | Recharts (admin dashboard — yalnızca lazy dashboard chunk'ında; ADR-033) |

## Kurulum ve Çalıştırma

```bash
cd frontend

# Bağımlılıkları yükle
npm install

# Ortam değişkenlerini hazırla (varsayılan backend portu 5153)
cp .env.example .env

# Geliştirme sunucusu (http://localhost:5173 — backend CORS bu origin'e açık)
npm run dev

# Prodüksiyon derlemesi (önce tsc tip kontrolü, sonra vite build → dist/)
npm run build

# Derlenmiş çıktıyı önizle
npm run preview
```

> Backend'in çalışıyor olması gerekir: `dotnet run --project src/Presentation/SigortaPro.WebAPI`
> (Development ortamında migration + seed otomatik uygulanır).

## Ortam Değişkenleri

| Değişken | Açıklama | Varsayılan |
|----------|----------|------------|
| `VITE_API_BASE_URL` | Backend API kök adresi (versiyon dahil) | `http://localhost:5153/api/v1` |

Yalnızca `VITE_` önekli değişkenler istemciye açılır; **gizli değer koymayın** (bundle'a gömülür).

## Klasör Yapısı

```
src/
├── app/                      ← Uygulama kabuğu
│   ├── App.tsx               ← RouterProvider
│   ├── providers.tsx         ← Provider kompozisyonu (QueryClientProvider + AuthProvider)
│   ├── routes.tsx            ← Rota ağacı (lazy + Suspense, layout slot kompozisyonu)
│   ├── ProtectedRoute.tsx    ← Rol tabanlı route guard (oturumsuz → /login, rol → /403)
│   ├── GuestRoute.tsx        ← Anonim rotalar (oturumlu → kendi alanı)
│   └── pages/                ← Sistem sayfaları (404, 401, 403, rol yönlendirme, yer tutucular)
├── features/
│   ├── auth/                 ← Kayıt/giriş/çıkış: pages, components (formlar, UserMenu),
│   │                            hooks (useAuth/useLogin/useRegister), services, types+schemas
│   ├── profile/              ← Profil + araç/konut yönetimi: ProfilePage, ProfileForm,
│   │                            VehicleForm/PropertyForm + panel'ler, hooks (useProfile), services
│   ├── quotes/               ← Teklif sihirbazı (wizard/), tekliflerim listesi/detayı,
│   │                            paket/prim/teminat bileşenleri, hooks (useQuotes), services
│   ├── payments/             ← Ödeme: PurchasePage, PaymentForm (kart + taksit),
│   │                            TestCardHints, PurchaseSuccess, hooks (usePayments), services
│   ├── policies/             ← Poliçelerim: PolicyListPage (durum sekmeleri), PolicyDetailPage
│   │                            (teminat tablosu), PolicyDocumentButton (PDF), hooks (usePolicies)
│   ├── claims/               ← Hasarlarım: ClaimCreatePage/ClaimForm, ClaimListPage,
│   │                            ClaimDetailPage + ClaimTimeline (durum zaman çizelgesi), hooks
│   ├── renewals/             ← Yenilemeler: RenewalListPage, RenewalCard (onay → ödeme köprüsü)
│   ├── dashboard/            ← Acente dashboard'u (personel): AdminDashboardPage — KPI kartları
│   │                            + Recharts grafikleri + en riskli müşteriler; hooks/services
│   └── customers/            ← Müşteri yönetimi (personel): AdminCustomerListPage — arama +
│                                il filtresi + detay çekmecesi; hooks/services
├── shared/
│   ├── components/           ← Tasarım sistemi (Button, Card, Input, Label, Select, Badge,
│   │                            Spinner, Alert, FormField, Textarea, Drawer, Pagination)
│   ├── hooks/                ← useDebounce (arama input'larında 300 ms debounce)
│   ├── layouts/              ← CustomerLayout (portal), AdminLayout (panel) — userMenu slot'lu
│   ├── lib/                  ← axios instance, queryClient, session, apiError, env, cn
│   ├── types/                ← Ortak API/auth/insurance tipleri (ProblemDetails, PagedResult,
│   │                            enum mirror'ları + etiket/rozet haritaları)
│   └── utils/                ← validation (TCKN/telefon/plaka), format (para/tarih/gün sayacı)
└── styles/globals.css        ← Tailwind + tasarım token'ları (HSL CSS değişkenleri)
```

## Mimari Notlar

- **API katmanı:** Tüm HTTP çağrıları `shared/lib/axios.ts` içindeki tek `api`
  instance'ı üzerinden yapılır. İstek interceptor'ı access token'ı ekler; 401'de
  yanıt interceptor'ı **tek uçuşlu (single-flight)** refresh yapar, isteği bir kez
  tekrarlar, yenileme de başarısızsa oturumu temizleyip `/401` sayfasına yönlendirir
  (bkz. `docs/ai/DECISIONS.md` ADR-028).
- **Oturum:** `shared/lib/session.ts` (localStorage, kalıcı doğruluk kaynağı) +
  `features/auth` içindeki `AuthProvider`/`useAuth` (React yansıması — ADR-029).
  Kayıt sonrası otomatik giriş yapılır; çıkış `UserMenu` üzerinden. Login sonrası,
  guard'ın bıraktığı `from` adresi rolle uyumluysa oraya dönülür.
- **Rota koruması:** `ProtectedRoute` (oturumsuz → `/login`, rol uyuşmazlığı → `/403`)
  ve `GuestRoute` (oturumlu kullanıcı login/register'dan kendi alanına) yalnızca UX
  yönlendirmesidir; gerçek yetki kontrolü backend'dedir (`[Authorize]` + kaynak
  sahipliği). Rol adları backend `UserRole` enum'u ile birebir aynıdır
  (`Admin`, `Personel`, `Customer`).
- **Form & doğrulama:** React Hook Form + Zod; şemalar backend FluentValidation
  kurallarını Türkçe mesajlarıyla aynalar (TCKN/telefon/plaka regex, aralık
  kontrolleri — `shared/utils/validation.ts`). Sunucu hataları `getApiErrorMessages`
  ile tek listeye indirgenip `Alert` içinde gösterilir. İş mantığı frontend'de
  yazılmaz (CLAUDE.md §10); doğrulamanın son sözü backend'dedir.
- **Enum'lar:** Backend Domain enum'ları JSON'da **sayısal** döner
  (`JsonStringEnumConverter` yok); `shared/types/insurance.types.ts` bunları sayısal
  mirror + Türkçe etiket/rozet haritaları olarak yansıtır ve API'ye sayısal gönderir
  (bkz. `docs/ai/DECISIONS.md` ADR-030).
- **Teklif sihirbazı:** `features/quotes` çok adımlı sihirbaz mevcut risk objesini
  seçtirir veya `features/profile` form/hook'larını **yeniden kullanarak** aynı
  ekranda ekletir (DRY); anlık prim + risk skoru + 3 paket tek `GET /quotes/compare`
  çağrısından gelir. `features/quotes` → `features/profile` bağımlılığı tek yönlüdür.
- **Ödeme & poliçe (Task 18 — ADR-031):** `features/payments` ödeme sayfası tek
  sayfalı durum makinesidir — yalnızca `Approved` teklif için taksit seçenekleri
  (`GET /payments/installment-options`) + kart formu gösterir, başarılı ödemede
  (`POST /payments`) aynı sayfada başarı ekranına geçer; ödeme reddi (402) `detail`
  mesajıyla gösterilir. `features/policies` "Poliçelerim" liste (durum sekmeleri:
  Aktif/Süresi Dolmuş/İptal/Tümü) + teminat tablolu detay (`CoverageList` teklif
  detayıyla ortaktır). **PDF indirme**, Bearer başlığı interceptor'da eklendiğinden
  `responseType:"blob"` ile yapılır (dosya adı `Content-Disposition`'dan). Backend'de
  poliçe listeleme/detay ucu olmadığından **iki salt okunur additive uç** eklendi
  (`GET /policies`, `GET /policies/{id}`; mevcut sözleşme değişmedi — ADR-031).
- **Hasar & yenileme (Task 19 — ADR-032):** `features/claims` hasar bildirim formu
  poliçe seçicisini **yalnızca aktif poliçelerden** (Task 18 `GET /policies?status=Active`)
  besler (DRY); mock foto yalnızca **dosya adı** metadatası olarak gönderilir (yükleme
  yok — ADR-024). Hasar detayı `ClaimTimeline` ile durum zaman çizelgesini gösterir
  (Bildirildi → İncelemede → Onaylandı → Ödendi | Reddedildi). `features/renewals`
  yenileme kartındaki **onay** (`POST /renewals/{id}/accept`) yeni teklifi Approved'a
  çeker ve "Ödemeye Geç" ile mevcut Task 18 ödeme akışına köprülenir. Cross-feature
  bağımlılıklar tek yönlü: `claims → policies`, `renewals → quotes`. Backend'e dokunulmadı.
- **Admin paneli (Task 20 — ADR-033):** `/admin` altında acente dashboard'u
  (`features/dashboard` — Task 14 uçlarından KPI kartları + **Recharts** ile aylık prim
  trendi/branş dağılımı; grafikler tema token'larını kullanır, Recharts yalnızca lazy
  dashboard chunk'ındadır) ve yönetim ekranları: **Müşteriler** (`features/customers`,
  debounce'lu arama + il filtresi), **Teklifler**/**Poliçeler**/**Hasarlar** admin sayfaları
  kendi domain feature'ında yaşar (ayrı "admin" çatısı yok; rozet/teminat/döküm bileşenleri
  iki yüzeyde ortak). Tüm listeler **tablo + filtre + detay çekmecesi** desenindedir (el
  yazımı `Drawer` + `Pagination`, shared). Personel poliçe listesi, personelin tüm
  poliçeleri görebildiği tek uç olan `GET /dashboard/reports/policies`'ten beslenir
  (`policies → dashboard`, tek yönlü). **Hasar karar akışı** (`ClaimDecisionPanel`) durum
  makinesini aynalar: incelemeye al → onayla (tutar+not) / reddet (gerekçe zorunlu) → öde;
  geçersiz geçişin son sözü backend'dedir (409). Backend'e dokunulmadı.
- **Tasarım sistemi:** shadcn/ui konvansiyonu — renkler `globals.css`'te HSL CSS
  değişkenleri, bileşen varyantları `class-variance-authority` ile. Koyu tema
  `<html class="dark">` ile hazır (toggle ileride). Feature bileşenleri (ör.
  `UserMenu`) layout'lara `userMenu` slot'u ile routes.tsx'ten enjekte edilir —
  `shared` katmanı feature'lara bağımlı olmaz (ADR-029).

## Rotalar

| Rota | Açıklama | Koruma |
|------|----------|--------|
| `/` | Role göre yönlendirme (oturum yoksa `/login`) | — |
| `/login` | Giriş (e-posta + şifre) | Anonim (oturumlu → kendi alanı) |
| `/register` | Müşteri kaydı (kayıt sonrası otomatik giriş) | Anonim (oturumlu → kendi alanı) |
| `/portal` | Müşteri portalı ana sayfası (hızlı erişim) | `Customer` |
| `/portal/profile` | Profil + araç/konut yönetimi | `Customer` |
| `/portal/quotes` | Tekliflerim (durum filtresi, geçerlilik sayacı) | `Customer` |
| `/portal/quotes/new` | Teklif sihirbazı (branş → risk → paket) | `Customer` |
| `/portal/quotes/:id` | Teklif detayı (prim dökümü, onayla/reddet, satın al) | `Customer` |
| `/portal/quotes/:id/purchase` | Ödeme sayfası (kart + taksit + test kartları → başarı ekranı) | `Customer` |
| `/portal/policies` | Poliçelerim (durum sekmeleri, sayfalama) | `Customer` |
| `/portal/policies/:id` | Poliçe detayı (teminat tablosu + PDF indirme) | `Customer` |
| `/portal/claims` | Hasarlarım (durum filtresi, sayfalama) | `Customer` |
| `/portal/claims/new` | Hasar bildirim formu (aktif poliçe + mock foto) | `Customer` |
| `/portal/claims/:id` | Hasar detayı (durum zaman çizelgesi) | `Customer` |
| `/portal/renewals` | Yenilemeler (yenileme teklifi kartı + onay) | `Customer` |
| `/admin` | Acente dashboard'u (KPI kartları + grafikler + en riskli müşteriler) | `Admin`, `Personel` |
| `/admin/customers` | Müşteri yönetimi (arama + il filtresi + detay çekmecesi) | `Admin`, `Personel` |
| `/admin/quotes` | Teklif yönetimi (durum/branş filtresi + detay çekmecesi) | `Admin`, `Personel` |
| `/admin/policies` | Poliçe yönetimi (tarih aralıklı rapor + detay çekmecesi + PDF) | `Admin`, `Personel` |
| `/admin/claims` | Hasar yönetimi (durum filtresi + detay çekmecesi + karar aksiyonları) | `Admin`, `Personel` |
| `/401` | Oturum süresi doldu (axios interceptor hedefi) | — |
| `/403` | Rol yetkisiz | — |
| `*` | 404 | — |

> Geliştirmede seed kullanıcılarıyla giriş yapılabilir (kök `README.md` "Seed Test
> Kullanıcıları"): `admin@sigortapro.com` / `Admin!2345` (panel),
> `musteri@sigortapro.com` / `Musteri!2345` (portal).
