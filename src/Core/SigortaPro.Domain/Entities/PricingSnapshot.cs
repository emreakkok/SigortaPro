using SigortaPro.Domain.Enums;

namespace SigortaPro.Domain.Entities;

/// <summary>
/// Teklifin fiyatlandığı andaki RİSK GİRDİLERİNİN dondurulmuş kopyası.
/// <para>
/// primi saklayıp dökümü yeniden hesaplar tarifeyi teklifte sabitler. Eksik olan halka,
/// motora giden <b>girdilerin</b> canlı (değiştirilebilir) entity'lerden okunmasıydı: müşteri ilini
/// değiştirdiğinde veya aracını güncellediğinde eski teklifin risk skoru ve prim dökümü kayıyordu.
/// Bu değer nesnesi, motorun ihtiyaç duyduğu <b>tüm primitifleri</b> teklif oluşturulurken kopyalar;
/// sonraki tüm yeniden hesaplar yalnızca buradan okur → geçmiş fiyat açıklaması matematiksel olarak sabitlenir.
/// </para>
/// <para>
/// Yalnızca fiyatı DOĞRUDAN belirleyen primitifler tutulur. Kişisel veri (TCKN, telefon, adresin tamamı,
/// ad-soyad) ve fiyatı etkilemeyen alanlar (plaka, marka, model) <b>bilinçli olarak taşınmaz</b> —
/// veri minimizasyonu (KVKK) ve amaç sınırlaması.
/// </para>
/// Branşa göre yalnızca ilgili alanlar dolar; diğerleri null kalır.
/// </summary>
public sealed class PricingSnapshot
{
    private PricingSnapshot()
    {
    }

    /// <summary>Kasko/Trafik girdileri.</summary>
    public static PricingSnapshot ForVehicle(
        DateTime capturedAt,
        int driverAge,
        int vehicleAge,
        int enginePowerHp,
        string riskCity,
        int noClaimTier,
        VehicleUsage? usagePurpose) => new()
        {
            CapturedAt = capturedAt,
            DriverAge = driverAge,
            VehicleAge = vehicleAge,
            EnginePowerHp = enginePowerHp,
            RiskCity = riskCity,
            NoClaimTier = noClaimTier,
            UsagePurpose = usagePurpose,
        };

    /// <summary>Konut/DASK girdileri.</summary>
    public static PricingSnapshot ForProperty(
        DateTime capturedAt, int buildingAge, int squareMeters, int earthquakeZone) => new()
        {
            CapturedAt = capturedAt,
            BuildingAge = buildingAge,
            SquareMeters = squareMeters,
            EarthquakeZone = earthquakeZone,
        };

    /// <summary>
    /// Sağlık girdileri. <paramref name="isSmoker"/> kullanıcı BEYANIDIR; varsayılan atanmaz —
    /// null, beyanın alınmadığı (eski) kayıt anlamına gelir ve faktör uygulanmaz/gösterilmez.
    /// </summary>
    public static PricingSnapshot ForHealth(DateTime capturedAt, int insuredAge, bool? isSmoker) => new()
        {
            CapturedAt = capturedAt,
            InsuredAge = insuredAge,
            IsSmoker = isSmoker,
        };

    // Girdilerin donduruldugu an (= teklifin fiyatlandığı an). ZORUNLU alan: hem denetim değeri taşır hem de
    // EF'in "snapshot var mı yok mu" ayrımını yapabilmesini sağlar (tüm diğer alanlar branşa göre null olabilir).
    public DateTime CapturedAt { get; private set; }

    // --- Araç (Kasko/Trafik) ---
    public int? DriverAge { get; private set; }
    public int? VehicleAge { get; private set; }
    public int? EnginePowerHp { get; private set; }

    // Aracın kullanım amacı beyanı, teklif anında dondurulur. null = beyan alınmadan (bu alan
    // eklenmeden) oluşmuş kayıt → faktör uygulanmaz ve prim dökümünde gösterilmez.
    public VehicleUsage? UsagePurpose { get; private set; }

    // Fiyatlamada kullanılan risk ili. MVP'de müşterinin adres ili vekil olarak kullanılır
    // (teknik borç: aracın tescil/kullanım ili ayrı tutulmuyor kayıtlı).
    public string? RiskCity { get; private set; }

    // Hasarsızlık basamağı. Şu an sistemde güvenilir biçimde türetilemediğinden daima 0'dır ve
    // fiyata etkisi yoktur; bu nedenle prim dökümünde de gösterilmez.
    public int? NoClaimTier { get; private set; }

    // --- Konut/DASK ---
    public int? BuildingAge { get; private set; }
    public int? SquareMeters { get; private set; }

    // Deprem bölgesi: kullanıcı beyanı değil, konut adresinin İLİNDEN türetilir.
    public int? EarthquakeZone { get; private set; }

    // --- Sağlık ---
    public int? InsuredAge { get; private set; }

    // Sigara kullanım beyanı. null = beyan alınmadan oluşturulmuş eski kayıt → faktör uygulanmaz
    // ve dökümde gösterilmez. Yeni sağlık tekliflerinde beyan zorunludur.
    public bool? IsSmoker { get; private set; }
}
