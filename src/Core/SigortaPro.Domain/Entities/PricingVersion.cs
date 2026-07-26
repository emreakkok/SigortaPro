using SigortaPro.Domain.Common;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Domain.Entities;

// ADR-048: Versiyonlanmış tarife (baz prim) kümesi. **Değişmezdir (immutable)**: oluşturulduktan sonra
// oranları/geçerlilik tarihi değiştirilemez — "fiyat değişikliği" her zaman YENİ bir versiyon oluşturur.
// Bu sayede (1) geçmiş fiyatlandırma kaydı ayrı bir audit sistemi kurmadan doğal olarak saklanır,
// (2) teklifler sabitledikleri versiyonla her zaman aynı primi yeniden üretir (ADR-021 determinizmi korunur).
public class PricingVersion : BaseEntity, IAggregateRoot
{
    protected PricingVersion()
    {
    }

    public PricingVersion(
        int versionNumber,
        DateTime effectiveFrom,
        string? note,
        Guid? createdByUserId,
        string? createdByName)
    {
        if (versionNumber <= 0)
        {
            throw new DomainException("Fiyatlandırma versiyon numarası pozitif olmalıdır.");
        }

        Id = Guid.NewGuid();
        VersionNumber = versionNumber;
        EffectiveFrom = effectiveFrom;
        Note = note;
        CreatedByUserId = createdByUserId;
        CreatedByName = createdByName;
    }

    // Artan sıra numarası — kullanıcıya "v3" gibi gösterilir ve aynı ana denk gelen versiyonlarda
    // deterministik sıralama (tie-break) sağlar.
    public int VersionNumber { get; private set; }

    // Bu tarifenin yürürlüğe girdiği an (UTC). Geçmişe tarihlenemez (Application katmanında doğrulanır).
    public DateTime EffectiveFrom { get; private set; }

    public string? Note { get; private set; }

    // Değişikliği yapan admin: stabil kimlik + o andaki görünen ad snapshot'ı (ADR-047 ile aynı gerekçe).
    public Guid? CreatedByUserId { get; private set; }
    public string? CreatedByName { get; private set; }

    public ICollection<PricingBranchRate> Rates { get; private set; } = new List<PricingBranchRate>();

    /// <summary>Versiyona bir branşın baz primini ekler. Yalnızca versiyon kurulurken kullanılır.</summary>
    public void SetRate(InsuranceBranch branch, decimal basePremium)
    {
        if (basePremium <= 0m)
        {
            throw new DomainException("Baz prim sıfırdan büyük olmalıdır.");
        }

        if (Rates.Any(rate => rate.Branch == branch))
        {
            throw new DomainException($"'{branch}' branşı için bu versiyonda zaten bir baz prim tanımlı.");
        }

        Rates.Add(new PricingBranchRate(Id, branch, basePremium));
    }

    /// <summary>Tarifenin eksiksiz olduğunu doğrular — kısmi tarife yayınlanamaz (fiyatlama boşluğu oluşmasın).</summary>
    public void EnsureCoversAllBranches()
    {
        var missing = Enum.GetValues<InsuranceBranch>()
            .Where(branch => Rates.All(rate => rate.Branch != branch))
            .ToList();

        if (missing.Count > 0)
        {
            throw new DomainException(
                $"Fiyatlandırma versiyonu tüm branşları içermelidir. Eksik: {string.Join(", ", missing)}.");
        }
    }
}
