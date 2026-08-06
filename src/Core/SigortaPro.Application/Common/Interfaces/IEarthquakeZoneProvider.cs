namespace SigortaPro.Application.Common.Interfaces;

// Deprem bölgesini konutun İLİNDEN türetir. Önceden bu değer kullanıcı tarafından serbestçe
// seçiliyordu ve fiyatı %33'e varan oranda doğrudan etkilediğinden beyana açık bir manipülasyon yüzeyiydi.
// Bölge artık doğrulanabilir bir olgudan (adres) türetilir.
public interface IEarthquakeZoneProvider
{
    // İl adına karşılık gelen deprem bölgesi (1 = en yüksek risk … 5 = en düşük).
    // İl tanınmıyorsa null döner — çağıran taraf SESSİZCE yanlış bölge atamaz; fiyatlama motoru
    // "bilinmeyen bölge" davranışını (orta risk) açık açıklamasıyla uygular.
    int? ResolveZone(string? city);
}
