using SigortaPro.Domain.Common;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Domain.Entities;

public class Quote : BaseEntity, IAggregateRoot
{
    protected Quote()
    {
    }

    public Quote(
        Guid customerId,
        Guid insuranceProductId,
        InsuranceBranch branch,
        Guid? vehicleId,
        Guid? propertyId,
        Guid? createdByStaffUserId = null)
    {
        ValidateRiskObject(branch, vehicleId, propertyId);

        Id = Guid.NewGuid();
        CustomerId = customerId;
        InsuranceProductId = insuranceProductId;
        Branch = branch;
        VehicleId = vehicleId;
        PropertyId = propertyId;
        CreatedByStaffUserId = createdByStaffUserId;
        Status = QuoteStatus.Draft;
    }

    public Guid CustomerId { get; private set; }
    public Customer? Customer { get; private set; }
    public Guid InsuranceProductId { get; private set; }
    public InsuranceProduct? InsuranceProduct { get; private set; }
    public Guid? VehicleId { get; private set; }
    public Vehicle? Vehicle { get; private set; }
    public Guid? PropertyId { get; private set; }
    public Property? Property { get; private set; }
    public InsuranceBranch Branch { get; private set; }
    public QuoteStatus Status { get; private set; }

    // ACENTE DESTEKLİ TEKLİF (agent-assisted): Teklifi müşteri ADINA oluşturan acente personelinin (Admin/Personel)
    // AppUser kimliği. null = müşteri teklifi kendi oluşturdu (self-service). Teklifin SAHİBİ her koşulda
    // CustomerId'dir (değişmez) — bu alan yalnızca "üreten personel"i (gerçek sigortacılıktaki "agent of record")
    // ayrı bir kavram olarak izler. Teklif kaynağı (SelfService/AgentAssisted) bu alandan TÜRETİLİR; ayrıca
    // saklanmaz (veri minimizasyonu). Yalnızca oluşturma anında (constructor) set edilir; sonradan değişmez.
    public Guid? CreatedByStaffUserId { get; private set; }
    public decimal TotalPremium { get; private set; }
    public DateTime? ValidUntil { get; private set; }

    // Seçilen teminat paketi (Standart varsayılan). Primi ve teminat limitlerini ölçekler; teklif
    // detayında prim dökümü bu seçim + saklı veri üzerinden deterministik yeniden hesaplanır (ADR-021).
    public CoveragePackage CoveragePackage { get; private set; } = CoveragePackage.Standart;

    // Hasar geçmişi çarpanı (varsayılan 1.00 = etkisiz). Yalnızca yenileme tekliflerinde (Task 13) müşterinin
    // önceki dönem hasar geçmişine göre 1.00'ın üzerine set edilir; prim dökümünün deterministik yeniden
    // hesabında CoveragePackage gibi saklı bir girdi olarak kullanılır (ADR-021 ile tutarlı — ADR-025).
    // ADR-059 (LEGACY): Bu alan artık YENİ tekliflerde kullanılmaz — hasar geçmişi tek bir Bonus-Malus
    // basamağıyla (PricingSnapshot.NoClaimTier) fiyatlanır. Alan, ADR-059 ÖNCESİ oluşturulmuş yenileme
    // tekliflerinin primini ve prim dökümünü birebir korumak için saklanır ve yeniden hesapta uygulanmaya
    // devam eder. Yeni tekliflerde varsayılan 1.00 (etkisiz) kalır; değeri değiştiren bir metot YOKTUR.
    public decimal ClaimHistoryFactor { get; private set; } = 1.00m;

    // YENİLEME İNDİRİMİ çarpanı (ADR-048 ailesi). Varsayılan 1.00 = indirim yok. Yalnızca YENİLEME teklifleri,
    // oluşturuldukları anda AKTİF tarife versiyonunun yenileme indirimini burada dondurur. Snapshot mantığıyla
    // aynı: değer teklifte saklandığından, tarife sonradan değişse bile bu teklifin primi/dökümü değişmez
    // (deterministik yeniden hesap — ADR-021). Yeni (yenileme dışı) tekliflerde 1.00 kalır → döküm değişmez.
    public decimal RenewalDiscountFactor { get; private set; } = 1.00m;

    // Sağlıkta "başkası adına" teklifte sigortalanan kişi (null = poliçe sahibi kendisi — ADR-041).
    // Owned/gömülü değer; fiyatlamanın deterministik yeniden hesabında yaş bu kişiden türetilir (ADR-021).
    public InsuredPerson? InsuredPerson { get; private set; }

    // ADR-048: Teklifin fiyatlandığı tarife versiyonu. Teklif oluşturulurken o an yürürlükte olan versiyon
    // **sabitlenir**; sonraki tüm yeniden hesaplar (detay, PDF, poliçe görünümü) bu versiyonu kullanır.
    // Böylece admin tarifeyi değiştirse bile geçmiş teklif/poliçe primleri matematiksel olarak değişemez.
    // null = bu alan eklenmeden önce oluşturulmuş kayıtlar → yerleşik baseline tarife (bit-aynı sonuç).
    public Guid? PricingVersionId { get; private set; }

    // ADR-053: Teklifin fiyatlandığı andaki risk GİRDİLERİ (sürücü/sigortalı yaşı, araç yaşı, motor gücü,
    // risk ili, bina yaşı, m², deprem bölgesi, sigara beyanı). ADR-048 tarifeyi sabitler; bu da girdiyi
    // sabitler → müşteri adresini/aracını sonradan değiştirse bile eski teklifin risk skoru ve prim dökümü
    // DEĞİŞMEZ. null = bu alan eklenmeden önce oluşturulmuş kayıtlar → canlı veriden hesaplanır (bit-aynı davranış).
    public PricingSnapshot? PricingSnapshot { get; private set; }

    /// <summary>
    /// Fiyatlama girdilerini teklifte dondurur; yalnızca fiyatlamadan önce (taslak) çağrılabilir.
    /// Tarife sabitleme (<see cref="PinPricingVersion"/>) ile birlikte teklifi fiyat açısından değişmez kılar.
    /// </summary>
    public void CapturePricingSnapshot(PricingSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (Status != QuoteStatus.Draft)
        {
            throw new DomainException("Fiyatlama girdileri yalnızca taslak durumundaki teklifte sabitlenebilir.");
        }

        PricingSnapshot = snapshot;
    }

    /// <summary>Teklifi fiyatlandıran tarife versiyonunu sabitler; yalnızca fiyatlamadan önce (taslak) çağrılabilir.</summary>
    public void PinPricingVersion(Guid pricingVersionId)
    {
        if (Status != QuoteStatus.Draft)
        {
            throw new DomainException("Fiyatlandırma versiyonu yalnızca taslak durumundaki teklifte sabitlenebilir.");
        }

        PricingVersionId = pricingVersionId;
    }

    /// <summary>
    /// Sağlık teklifinde sigortalıyı poliçe sahibinden farklı bir kişi olarak belirler ("başkası adına").
    /// Yalnızca Sağlık branşında ve taslak durumunda (fiyatlamadan önce) çağrılabilir.
    /// </summary>
    public void SetInsuredPerson(InsuredPerson insuredPerson)
    {
        if (Branch != InsuranceBranch.Saglik)
        {
            throw new DomainException("Başkası adına sigortalı yalnızca Sağlık branşında tanımlanabilir.");
        }

        if (Status != QuoteStatus.Draft)
        {
            throw new DomainException("Sigortalı bilgisi yalnızca taslak durumundaki teklifte belirlenebilir.");
        }

        InsuredPerson = insuredPerson;
    }

    /// <summary>
    /// Yenileme indirimini (aktif tarifeden) teklifte dondurur; yalnızca fiyatlamadan önce (taslak) çağrılabilir.
    /// factor ∈ (0, 1]: 1.00 = indirim yok, 0.90 = %10 yenileme indirimi.
    /// </summary>
    public void ApplyRenewalDiscount(decimal factor)
    {
        if (Status != QuoteStatus.Draft)
        {
            throw new DomainException("Yenileme indirimi yalnızca taslak durumundaki teklifte uygulanabilir.");
        }

        if (factor <= 0m || factor > 1.00m)
        {
            throw new DomainException("Yenileme indirim çarpanı (0, 1] aralığında olmalıdır.");
        }

        RenewalDiscountFactor = factor;
    }

    /// <summary>Fiyatlamadan önce teminat paketini seçer; yalnızca taslak durumunda değiştirilebilir.</summary>
    public void SelectCoveragePackage(CoveragePackage coveragePackage)
    {
        if (Status != QuoteStatus.Draft)
        {
            throw new DomainException("Teminat paketi yalnızca taslak durumundaki teklifte seçilebilir.");
        }

        CoveragePackage = coveragePackage;
    }

    /// <summary>Draft → Priced. Fiyatlama motorunun ürettiği tutar ve geçerlilik tarihiyle çağrılır.</summary>
    public void MarkAsPriced(decimal totalPremium, DateTime validUntil)
    {
        if (Status != QuoteStatus.Draft)
        {
            throw new DomainException("Yalnızca taslak durumundaki teklifler fiyatlandırılabilir.");
        }

        TotalPremium = totalPremium;
        ValidUntil = validUntil;
        Status = QuoteStatus.Priced;
    }

    /// <summary>Priced → Approved. Müşteri paketi onayladığında çağrılır.</summary>
    public void Approve()
    {
        if (Status != QuoteStatus.Priced)
        {
            throw new DomainException("Yalnızca fiyatlandırılmış teklifler onaylanabilir.");
        }

        Status = QuoteStatus.Approved;
    }

    /// <summary>Approved → Purchased. Ödeme başarılı olduğunda çağrılır.</summary>
    public void Purchase()
    {
        if (Status != QuoteStatus.Approved)
        {
            throw new DomainException("Yalnızca onaylanmış teklifler satın alınabilir.");
        }

        Status = QuoteStatus.Purchased;
    }

    /// <summary>Müşteri teklifi reddettiğinde çağrılır; satın alınmış veya süresi dolmuş teklif reddedilemez.</summary>
    public void Reject()
    {
        if (Status is QuoteStatus.Purchased or QuoteStatus.Expired)
        {
            throw new DomainException("Satın alınmış veya süresi dolmuş teklif reddedilemez.");
        }

        Status = QuoteStatus.Rejected;
    }

    /// <summary>Geçerlilik süresi dolan teklifleri arkaplan servisi bu metotla Expired durumuna çeker.</summary>
    public void Expire(DateTime now)
    {
        if (Status is QuoteStatus.Purchased or QuoteStatus.Rejected or QuoteStatus.Expired)
        {
            return;
        }

        if (ValidUntil is null || now < ValidUntil)
        {
            throw new DomainException("Teklifin geçerlilik süresi henüz dolmamış.");
        }

        Status = QuoteStatus.Expired;
    }

    private static void ValidateRiskObject(InsuranceBranch branch, Guid? vehicleId, Guid? propertyId)
    {
        var requiresVehicle = branch is InsuranceBranch.Kasko or InsuranceBranch.Trafik;
        var requiresProperty = branch is InsuranceBranch.Konut or InsuranceBranch.Dask;

        if (requiresVehicle && vehicleId is null)
        {
            throw new DomainException("Kasko/Trafik teklifi için araç bilgisi zorunludur.");
        }

        if (requiresProperty && propertyId is null)
        {
            throw new DomainException("Konut/DASK teklifi için konut bilgisi zorunludur.");
        }
    }
}
