using SigortaPro.Domain.Common;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Domain.Entities;

public class Quote : BaseEntity, IAggregateRoot
{
    protected Quote()
    {
    }

    public Quote(Guid customerId, Guid insuranceProductId, InsuranceBranch branch, Guid? vehicleId, Guid? propertyId)
    {
        ValidateRiskObject(branch, vehicleId, propertyId);

        Id = Guid.NewGuid();
        CustomerId = customerId;
        InsuranceProductId = insuranceProductId;
        Branch = branch;
        VehicleId = vehicleId;
        PropertyId = propertyId;
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
    public decimal TotalPremium { get; private set; }
    public DateTime? ValidUntil { get; private set; }

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
