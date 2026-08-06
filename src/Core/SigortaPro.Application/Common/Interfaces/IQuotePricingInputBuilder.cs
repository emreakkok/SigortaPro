using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Common.Interfaces;

// Fiyatlama girdisini (PricingSnapshot) kuran tek nokta. Teklif oluşturma ve karşılaştırma
// önizlemesi AYNI yolu kullanır → gösterilen fiyat ile oluşturulan teklifin fiyatı yapısal olarak eşittir.
//
// TASKS.md'de tanımlı domain terimi ("konut risk objesi" = property) korunuyor; CA1716 (VB.NET 'Property'
// anahtar sözcüğü çakışması) Property entity'sindeki ile aynı gerekçeyle bilinçli olarak suppress edilmiştir.
#pragma warning disable CA1716
public interface IQuotePricingInputBuilder
{
    /// <summary>
    /// Verilen risk verisinden fiyatlama motorunun girdisini (dondurulmuş primitifler) kurar.
    /// Önizlemede sonuç kalıcılaştırılmaz; oluşturmada teklife eklenir — her iki durumda da AYNI değerlerdir.
    /// </summary>
    Task<PricingSnapshot> BuildAsync(
        InsuranceBranch branch,
        Customer customer,
        Vehicle? vehicle,
        Property? property,
        DateTime referenceDate,
        DateTime? insuredBirthDate,
        bool? isSmoker,
        CancellationToken cancellationToken = default);
}
#pragma warning restore CA1716
