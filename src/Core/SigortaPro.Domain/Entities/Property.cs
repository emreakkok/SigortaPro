using SigortaPro.Domain.Common;

namespace SigortaPro.Domain.Entities;

// TASKS.md'de tanımlı domain terimi ("konut risk objesi") korunuyor; CA1716 (VB.NET 'Property' anahtar sözcüğü çakışması) bilinçli olarak suppress edilmiştir.
#pragma warning disable CA1716
public class Property : BaseEntity, IAggregateRoot
#pragma warning restore CA1716
{
    protected Property()
    {
    }

    /// <param name="derivedEarthquakeZone">
    /// Sistem tarafından <b>adresin ilinden türetilen</b> deprem bölgesi. Kullanıcı beyanı DEĞİLDİR;
    /// bu değeri çözmek çağıranın (Application katmanı, <c>IEarthquakeZoneProvider</c>) sorumluluğundadır.
    /// Çözülemeyen ilde <c>EarthquakeZoneDefaults.Unknown</c> geçilir.
    /// </param>
    public Property(
        Guid customerId, Address address, int buildingAge, int squareMeters, int derivedEarthquakeZone)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        Address = address;
        BuildingAge = buildingAge;
        SquareMeters = squareMeters;
        EarthquakeZone = derivedEarthquakeZone;
    }

    public Guid CustomerId { get; private set; }
    public Customer? Customer { get; private set; }
    public Address Address { get; private set; } = null!;
    public int BuildingAge { get; private set; }
    public int SquareMeters { get; private set; }
    /// <summary>
    /// Kayıt anında belirlenen deprem bölgesi (1 = en yüksek risk … 5 = en düşük; 0 = bilinmiyor).
    /// <para>
    /// itibaren bu değer <b>yalnızca sistem tarafından</b> adresin ilinden türetilir.
    /// ÖNCESİ kaydedilmiş konutlarda ise müşterinin o gün yaptığı <b>beyandır</b> ve tarihsel
    /// doğruluk için <b>olduğu gibi korunur</b> (geriye dönük düzeltilmez).
    /// </para>
    /// <para>
    /// <b>Yeni tekliflerin fiyatı bu alandan okunmaz:</b> fiyatlama girdisi her seferinde adresin ilinden
    /// yeniden türetilir (<c>QuotePricingInputBuilder</c>). Bu alan, snapshot'ı olmayan ESKİ tekliflerin
    /// yeniden hesabında ve gösterimde kullanılır. Bu yüzden değeri değiştirmek, geçmiş tekliflerin
    /// prim dökümünü bozardı.
    /// </para>
    /// Bölgeyi sonradan değiştirecek bir domain metodu <b>bilinçli olarak yoktur</b>.
    /// </summary>
    public int EarthquakeZone { get; private set; }
}
