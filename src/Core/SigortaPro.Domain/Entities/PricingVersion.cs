using SigortaPro.Domain.Common;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Domain.Entities;

// ADR-048: Versiyonlanmış tarife. Yaşam döngüsü: Taslak (Draft) → Aktif (Active) → Arşiv (Archived).
//  • TASLAK serbestçe düzenlenebilir ve HİÇBİR teklifi fiyatlamaz (canlı fiyatları etkilemez).
//  • AKTİF versiyon DEĞİŞMEZDİR; yeni teklifler onu kullanır ve kendilerinde sabitler (pin).
//  • Yeni bir versiyon aktifleştirildiğinde eskisi ARŞİVLENİR (yine değişmez) → geçmiş teklif/poliçe primleri
//    sabitledikleri versiyonla her zaman aynı sonucu üretir (ADR-021 determinizmi korunur).
// Aynı anda yalnızca BİR aktif versiyon bulunur (aktifleştirme handler'ı bu tekilliği uygular).
public class PricingVersion : BaseEntity, IAggregateRoot
{
    protected PricingVersion()
    {
    }

    // Yeni versiyon her zaman TASLAK olarak doğar. name zorunludur (oluştururken alınır); effectiveFrom
    // geçerlilik başlangıcıdır (varsayılan: oluşturma anı, taslakta düzenlenebilir).
    public PricingVersion(
        int versionNumber,
        string name,
        DateTime effectiveFrom,
        string? note,
        Guid? createdByUserId,
        string? createdByName)
    {
        if (versionNumber <= 0)
        {
            throw new DomainException("Fiyatlandırma versiyon numarası pozitif olmalıdır.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Taslak adı zorunludur.");
        }

        Id = Guid.NewGuid();
        VersionNumber = versionNumber;
        Name = name.Trim();
        EffectiveFrom = effectiveFrom;
        Note = note;
        CreatedByUserId = createdByUserId;
        CreatedByName = createdByName;
        Status = PricingVersionStatus.Draft;
    }

    // Artan sıra numarası — kullanıcıya "v3" gibi gösterilir ve deterministik sıralama (tie-break) sağlar.
    public int VersionNumber { get; private set; }

    // Kullanıcı tanımlı taslak adı (oluştururken zorunlu). Eski kayıtlarda null olabilir (migration öncesi).
    public string? Name { get; private set; }

    // Geçerlilik başlangıcı (admin girer; taslakta düzenlenebilir). ActivatedAt = sistemsel aktifleşme anı (ayrı).
    public DateTime EffectiveFrom { get; private set; }

    // Opsiyonel geçerlilik bitişi (metadata; boş = süresiz).
    public DateTime? EffectiveTo { get; private set; }

    // Versiyonun AKTİFLEŞTİRİLDİĞİ an (Activate ile set edilir). Taslak/hiç aktifleşmemişse null.
    public DateTime? ActivatedAt { get; private set; }

    public string? Note { get; private set; }

    // Değişikliği yapan admin: stabil kimlik + o andaki görünen ad snapshot'ı (ADR-047 ile aynı gerekçe).
    public Guid? CreatedByUserId { get; private set; }
    public string? CreatedByName { get; private set; }

    // Yaşam döngüsü durumu (Draft/Active/Archived).
    public PricingVersionStatus Status { get; private set; }

    // Baz prim dışındaki ticari kaldıraçlar (paket çarpanları, il katsayıları, yenileme indirimi). null =
    // bu alan eklenmeden önce oluşmuş versiyon → motor yerleşik baseline katsayılarını kullanır (eski teklifler
    // bit-aynı). EF'te tek JSON kolonuna serileştirilir (owned value object).
    public PricingRuleSet? RuleSet { get; private set; }

    public ICollection<PricingBranchRate> Rates { get; private set; } = new List<PricingBranchRate>();

    /// <summary>Versiyona bir branşın baz primini ekler. Yalnızca taslak düzenlenirken kullanılır.</summary>
    public void SetRate(InsuranceBranch branch, decimal basePremium)
    {
        EnsureDraft();

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

    /// <summary>Taslağın ticari kaldıraç setini (paket/şehir/yenileme) belirler. Yalnızca taslakta.</summary>
    public void SetRuleSet(PricingRuleSet ruleSet)
    {
        EnsureDraft();
        RuleSet = ruleSet ?? throw new DomainException("Kural seti boş olamaz.");
    }

    /// <summary>
    /// Taslağı topluca günceller: baz primler + kural seti + açıklama. Yalnızca taslak durumunda çağrılabilir;
    /// aktif/arşiv versiyon asla değişmez (geçmiş primler korunur).
    /// </summary>
    public void UpdateDraft(
        string name,
        DateTime effectiveFrom,
        DateTime? effectiveTo,
        string? note,
        PricingRuleSet ruleSet,
        IEnumerable<KeyValuePair<InsuranceBranch, decimal>> rates)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(rates);

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Taslak adı zorunludur.");
        }

        if (effectiveTo is not null && effectiveTo <= effectiveFrom)
        {
            throw new DomainException("Geçerlilik bitişi, başlangıcından sonra olmalıdır.");
        }

        Name = name.Trim();
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        Note = note;
        RuleSet = ruleSet ?? throw new DomainException("Kural seti boş olamaz.");

        // Satırları YERİNDE güncelle (sil+ekle yerine): aynı (versiyon, branş) benzersiz anahtarında EF'in
        // tek SaveChanges içinde delete/insert sıralaması çakışması yaşamaması için. Yeni branş eklenir,
        // artık olmayan branş kaldırılır.
        var incoming = rates.ToDictionary(rate => rate.Key, rate => rate.Value);

        foreach (var existing in Rates.Where(rate => !incoming.ContainsKey(rate.Branch)).ToList())
        {
            Rates.Remove(existing);
        }

        foreach (var (branch, basePremium) in incoming)
        {
            var existing = Rates.FirstOrDefault(rate => rate.Branch == branch);
            if (existing is null)
            {
                Rates.Add(new PricingBranchRate(Id, branch, basePremium));
            }
            else
            {
                existing.SetBasePremium(basePremium);
            }
        }

        EnsureCoversAllBranches();
    }

    /// <summary>Taslak → Aktif. Aktifleşme anı (ActivatedAt) sabitlenir; kullanıcının girdiği EffectiveFrom
    /// korunur. Aktif versiyon artık değiştirilemez.</summary>
    public void Activate(DateTime now)
    {
        if (Status != PricingVersionStatus.Draft)
        {
            throw new DomainException("Yalnızca taslak durumundaki tarife versiyonu aktifleştirilebilir.");
        }

        EnsureCoversAllBranches();
        ActivatedAt = now;
        Status = PricingVersionStatus.Active;
    }

    /// <summary>Aktif → Arşiv. Yeni bir versiyon aktifleştirildiğinde eskisi bu duruma geçer.</summary>
    public void Archive()
    {
        if (Status != PricingVersionStatus.Active)
        {
            throw new DomainException("Yalnızca aktif tarife versiyonu arşivlenebilir.");
        }

        Status = PricingVersionStatus.Archived;
    }

    /// <summary>Tarifenin eksiksiz olduğunu doğrular — kısmi tarife aktifleştirilemez (fiyatlama boşluğu oluşmasın).</summary>
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

    private void EnsureDraft()
    {
        if (Status != PricingVersionStatus.Draft)
        {
            throw new DomainException("Yalnızca taslak durumundaki tarife versiyonu düzenlenebilir.");
        }
    }
}
