namespace SigortaPro.Domain.Enums;

// Bir tarife (fiyatlandırma) versiyonunun yaşam döngüsü. Gerçek sigortacılıkta tarife "hazırlanır (taslak) →
// yürürlüğe alınır (aktif) → yeni tarife gelince arşivlenir." Aynı anda YALNIZCA BİR aktif versiyon bulunur;
// yeni teklifler her zaman aktif versiyonu kullanır ve onu kendilerinde sabitler (pin) → geçmiş primler değişmez.
public enum PricingVersionStatus
{
    // Hazırlanıyor. Serbestçe düzenlenebilir; HİÇBİR teklif bu versiyonla fiyatlanmaz (canlı fiyatları etkilemez).
    Draft = 0,

    // Yürürlükte. Bu andan sonra oluşturulan teklifler bu versiyonu kullanır. Değişmezdir (düzenlenemez).
    Active = 1,

    // Arşiv. Bir zamanlar aktifti; yeni bir versiyon aktifleştirildiğinde buraya geçer. Yalnızca geçmiş
    // teklif/poliçelerin sabitlediği tarife olarak yaşamaya devam eder (deterministik yeniden hesap). Değişmez.
    Archived = 2,
}
