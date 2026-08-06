# SigortaPro — Fiyatlama Kuralları (Mock)

> **Kapsam:** — Risk Analizi & Dinamik Fiyatlama Motoru (Mock).
> **Karar:** (``, yerel geliştirme dokümanı).
> **Uyarı:** Bu kurallar **demo/MVP** amaçlıdır; gerçek aktüeryal doğruluğu yoktur.
> **Senkronizasyon:** Buradaki tüm değerler `SigortaPro.Infrastructure/Services/Pricing/` (`PricingEngine`, `PricingRuleTables`) ile **birebir** eşleşir. Kural değiştiğinde iki taraf birlikte güncellenir.

---

## 1. Hesaplama Modeli

```
ToplamPrim = BazPrim × (tüm risk faktörü çarpanlarının çarpımı)
```

- Sonuç **2 ondalığa** yuvarlanır (`MidpointRounding.AwayFromZero`, `decimal(18,2)`).
- **Toplam çarpan** = `ToplamPrim / BazPrim`; risk skoru bu değere göre belirlenir.
- Motor **saf/deterministik** bir fonksiyondur: aynı girdi her zaman aynı çıktıyı üretir. Sürücü yaşı, araç yaşı gibi değerler motora **önceden hesaplanmış** olarak verilir (motor sistem saatine, domain entity'lerine veya Quote akışına bağımlı değildir).

### Risk Skoru Eşikleri (toplam çarpan)

| Toplam Çarpan | Risk Skoru |
|---------------|-----------|
| `< 1.10` | **Low** |
| `1.10 – 1.50` (hariç) | **Medium** |
| `≥ 1.50` | **High** |

### Baz Primler (yıllık, TRY)

| Branş | Baz Prim |
|-------|----------|
| Kasko | 15.000 |
| Trafik | 6.000 |
| Konut | 3.000 |
| DASK | 1.500 |
| Sağlık | 8.000 |

---

## 2. Kasko / Trafik Faktörleri (Risk Objesi: Araç)

### Sürücü Yaşı
| Koşul | Çarpan |
|-------|--------|
| < 25 | 1.30 |
| 25 – 65 | 1.00 |
| > 65 | 1.15 |

### Araç Yaşı
| Koşul | Çarpan |
|-------|--------|
| 0 – 3 | 1.15 |
| 4 – 10 | 1.00 |
| > 10 | 0.85 |

### Motor Gücü (HP)
| Koşul | Çarpan |
|-------|--------|
| ≤ 100 | 1.00 |
| 101 – 160 | 1.10 |
| 161 – 240 | 1.25 |
| > 240 | 1.45 |

### Kullanım Amacı (Beyan)
| Kullanım | Çarpan |
|----------|--------|
| Hususi | 1.00 |
| Ticari | 1.30 |
| Taksi | 1.60 |

> **Durum:** Kullanım amacı, araç kaydında **zorunlu bir kullanıcı beyanıdır** (varsayılan yoktur).
> Yalnızca **Kasko/Trafik** fiyatlamasını etkiler; Konut/DASK/Sağlık bu bilgiyi kullanmaz. Teklif anında
> `PricingSnapshot.UsagePurpose` olarak dondurulur → araç sonradan güncellense bile eski teklifin primi ve
> dökümü değişmez. Beyanı olmayan (bu alan eklenmeden kaydedilmiş) araçlarda faktör **uygulanmaz ve dökümde
> gösterilmez** — geçmişe dönük uygulanmaz.
>
> ⚠️ **Katsayılar MVP SİMÜLASYONUDUR** — gerçek aktüeryal tarife verisi değildir. Sıralama gerçek hayattaki
> maruziyet farkını (yıllık kilometre/kaza sıklığı) temsil eder; mutlak değerler örnek amaçlıdır.

### İl Risk Katsayısı
| İl | Çarpan |
|----|--------|
| İstanbul | 1.25 |
| İzmir | 1.20 |
| Ankara | 1.15 |
| Bursa | 1.10 |
| Antalya | 1.10 |
| Diğer (varsayılan) | 1.00 |

> Eşleşme büyük/küçük harf duyarsızdır; listede olmayan iller varsayılan katsayıyı alır (mock — il alanı serbest metindir).

### Hasarsızlık Basamağı — Bonus-Malus 

Hasar geçmişinin **tek** çarpanıdır. Negatif basamak ek prim (**malus**), pozitif basamak indirim (**bonus**),
0 nötrdür. Yalnızca **Kasko ve Trafik** fiyatlamasında uygulanır; her branş **kendi** basamağını taşır.

```
Basamak = clamp( (hasarsız tamamlanmış dönem) − 2 × (onaylanmış/ödenmiş hasar), −3, +6 )
```

| Basamak | Çarpan | | Basamak | Çarpan |
|---|---|---|---|---|
| −3 | 1.60 | | +2 | 0.90 |
| −2 | 1.40 | | +3 | 0.85 |
| −1 | 1.20 | | +4 | 0.80 |
| **0** | **1.00** (yeni müşteri) | | +5 | 0.75 |
| +1 | 0.95 | | +6 | 0.70 |

- **Yeni müşteri 0. basamaktan başlar.** SigortaPro dışındaki geçmiş bilinmediğinden **varsayılmaz** —
 ne indirim ne ceza verilir. Müşteri geçmişini beyan **edemez** (beyana açık indirim güvenlik açığıdır).
- **Yalnızca `Approved`/`Paid` hasarlar** sayılır; `Submitted`/`UnderReview`/`Rejected` basamağı etkilemez.
- **Malus sönümlenir:** hasarlı müşteri sonraki hasarsız dönemlerde kademeli olarak toparlanır.
- Basamak **durumsuz** hesaplanır (her fiyatlamada mevcut veriden yeniden türetilir) ve teklif anında
 `PricingSnapshot.NoClaimTier`'a **dondurulur** → sonradan hasar oluşsa bile eski teklif değişmez.
- Basamak **0 iken prim dökümünde kalem gösterilmez** (etkisiz kalem sunulmaz; eski kayıtların dökümü de korunur).
- ⚠️ **Katsayılar MVP SİMÜLASYONUDUR** — gerçek aktüeryal tarife verisi değildir.

> **Emekliye ayrılan `ClaimHistoryFactor` (LEGACY):** Önceden yenilemede ayrı bir hasar çarpanı
> (`1.00 + 0.20×hasar`, tavan 1.60) uygulanıyordu. İki bağımsız ölçek aralarında hiçbir değişmez olmadığından
> **çelişkili sonuç üretebiliyordu** (ör. 3 hasarlı + yüksek basamaklı müşteri ≈ nötr fiyat). ile hasar
> geçmişi tek basamağa indirildi. `Quote.ClaimHistoryFactor` alanı **silinmedi**: öncesi oluşmuş
> yenileme tekliflerinin primi ve dökümü birebir korunsun diye saklanır ve yeniden hesapta uygulanmaya devam eder.
> Yeni tekliflerde daima 1.00'dır (değeri değiştiren bir metot yoktur).

### Bina Yaşı
| Koşul | Çarpan |
|-------|--------|
| 0 – 5 | 0.95 |
| 6 – 20 | 1.00 |
| 21 – 40 | 1.10 |
| > 40 | 1.25 |

### Metrekare
| Koşul | Çarpan |
|-------|--------|
| ≤ 75 | 0.90 |
| 76 – 120 | 1.00 |
| 121 – 200 | 1.15 |
| > 200 | 1.30 |

### Deprem Bölgesi (1 = en yüksek risk … 5 = en düşük)

> **Kaynak:** Bölge **kullanıcı beyanı değildir**; konutun **ilinden** türetilir
> (gömülü il→bölge eşlemesi). Bu, il düzeyinde bir **MVP yaklaşıklamasıdır** — gerçek tehlike haritası
> ilçe/koordinat düzeyindedir. İl tanınmazsa bölge atanmaz ve motor "bilinmeyen bölge" (1.15) davranışını
> açık açıklamasıyla uygular; sessizce (ve müşteri lehine) bir bölge **atanmaz**.
| Bölge | Çarpan |
|-------|--------|
| 1 | 1.50 |
| 2 | 1.30 |
| 3 | 1.15 |
| 4 | 1.05 |
| 5 | 1.00 |

---

## 4. Sağlık Faktörleri (Risk Objesi: Kişi)

### Yaş Bandı
| Koşul | Çarpan |
|-------|--------|
| 0 – 17 | 0.80 |
| 18 – 30 | 1.00 |
| 31 – 45 | 1.15 |
| 46 – 60 | 1.40 |
| > 60 | 1.80 |

### Sigara Kullanımı (Beyan)
| Beyan | Çarpan |
|-------|--------|
| Kullanıyor | 1.25 |
| Kullanmıyor | 1.00 |

> **Durum:** Beyan **teklif sihirbazında zorunlu olarak alınır** (varsayılan yoktur; seçim
> yapılmadan devam edilemez) ve teklifte `PricingSnapshot.IsSmoker` olarak saklanır. Beyan alınmamış eski
> kayıtlarda faktör **uygulanmaz ve dökümde gösterilmez**. Yalnızca fiyatlama amacıyla kullanılır;
> bildirimlere, aktivite akışına ve admin listelerine taşınmaz (KVKK — veri minimizasyonu).

---

## 5. Örnek Hesaplamalar

**Kasko — bileşik yüksek risk:**
Sürücü 22 (1.30) × Araç 2 yaş (1.15) × 250 HP (1.45) × İstanbul (1.25) × hasarsızlık 0 (1.00)
= `2.7096875` → `15.000 × 2.7096875 = 40.645,31 TRY` → **High**.

**Konut — yüksek risk:**
Bina 50 yaş (1.25) × 250 m² (1.30) × 1. bölge (1.50)
= `2.4375` → `3.000 × 2.4375 = 7.312,50 TRY` → **High**.

**Sağlık — orta yaş, sigara:**
35 yaş (1.15) × sigara (1.25) = `1.4375` → `8.000 × 1.4375 = 11.500,00 TRY` → **Medium**.

---

## 6. Teminat Paketleri 

Aynı risk objesi için sunulan teminat seviyeleri; hem primi hem teminat limitlerini ölçekler. Paket **risk skorunu etkilemez** (kapsam seçimidir, risk faktörü değil). Katsayılar `SigortaPro.Application/Features/Quotes/CoveragePackageFactors` ile birebir eşleşir.

| Paket | Prim Çarpanı | Teminat Limiti Çarpanı |
|-------|--------------|------------------------|
| Standart | 1.00 | 1.00 |
| Genişletilmiş | 1.30 | 1.50 |
| Premium | 1.60 | 2.00 |

```
TeklifPrimi = MotorPrimi (Bölüm 1-4) × PaketPrimÇarpanı
TeminatLimiti = ÜrünVarsayılanLimiti × PaketLimitÇarpanı
```

> **Örnek:** Standart Kasko primi 40.645,31 TRY ise, Genişletilmiş paket = `40.645,31 × 1.30 = 52.838,90 TRY` ve tüm teminat limitleri ×1.50 uygulanır.

---

## 6.1. Yenileme Hasar Çarpanı — ⚠️ EMEKLİ 

> Yeni tekliflerde **uygulanmaz**; yerini Bonus-Malus basamağı aldı. Yalnızca öncesi kayıtların determinizmi için korunur.

> **Kapsam:** Hasar sayımı **branşa göre kapsanır** — bir Kasko hasarı Sağlık yenilemesini
> pahalılaştıramaz. Hasarın branşı, bağlı olduğu poliçenin teklifinden (`Policy → Quote.Branch`) belirlenir.

Poliçe yenileme tekliflerinde, müşterinin **fiyatlamaya etki eden hasar geçmişi** prime ek çarpan olarak yansır. Fiyatlamaya etki eden hasar = `Approved` **veya** `Paid` durumundaki hasarlar. Her hasar %20 ek prim getirir; en fazla 3 hasara kadar birikir (tavan +%60). Hasarsız müşteride çarpan 1.00'dır (etkisiz). Katsayılar `SigortaPro.Application/Features/Renewals/RenewalPricing` ile birebir eşleşir.

| Fiyatlamaya Etki Eden Hasar Sayısı | Hasar Çarpanı |
|------------------------------------|---------------|
| 0 | 1.00 |
| 1 | 1.20 |
| 2 | 1.40 |
| 3 veya üzeri | 1.60 |

```
YenilemePrimi = MotorPrimi (Bölüm 1-4) × PaketPrimÇarpanı × HasarÇarpanı
```

> Hasar çarpanı yalnızca yenileme akışında (arkaplan servisi) uygulanır ve teklifte `Quote.ClaimHistoryFactor` olarak saklanır; bu sayede prim dökümü, teminat paketi gibi deterministik olarak yeniden hesaplanır. Normal (ilk) tekliflerde çarpan 1.00'dır.
>
> **Örnek:** Yenileme motor+paket primi 20.625 TRY olan, 1 ödenmiş hasarı bulunan bir müşteride yenileme primi = `20.625 × 1.20 = 24.750 TRY`.

---

## 7. Sözleşme (Arayüz)

- **Arayüz:** `IPricingEngine.CalculatePremium(PricingRequest)` — `SigortaPro.Application/Common/Interfaces`.
- **Girdi:** `VehiclePricingRequest` (Kasko/Trafik), `PropertyPricingRequest` (Konut/DASK), `HealthPricingRequest` (Sağlık) — `Common/Pricing`.
- **Çıktı:** `PricingResult` (`BasePremium`, `TotalPremium`, `RiskScore`, `Breakdown[]`).
- Girdi tipi ile branş uyuşmazsa (örn. araç isteğine Konut branşı) `ArgumentException` fırlatılır.
- Girdilerin aralık doğrulaması (yaş, metrekare vb.) çağıran katmanın sorumluluğudur; teklif oluşturma komutu bu doğrulamayı FluentValidation ile yapacaktır.
