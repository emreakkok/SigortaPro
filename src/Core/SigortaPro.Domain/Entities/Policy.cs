using SigortaPro.Domain.Common;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Domain.Entities;

public class Policy : BaseEntity, IAggregateRoot
{
    protected Policy()
    {
    }

    public Policy(string policyNumber, Guid customerId, Guid quoteId, DateTime startDate, DateTime endDate, decimal totalPremium)
    {
        if (endDate <= startDate)
        {
            throw new DomainException("Poliçe bitiş tarihi başlangıç tarihinden sonra olmalıdır.");
        }

        Id = Guid.NewGuid();
        PolicyNumber = policyNumber;
        CustomerId = customerId;
        QuoteId = quoteId;
        StartDate = startDate;
        EndDate = endDate;
        TotalPremium = totalPremium;
        Status = PolicyStatus.Active;
    }

    public string PolicyNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public Customer? Customer { get; private set; }
    public Guid QuoteId { get; private set; }
    public Quote? Quote { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public decimal TotalPremium { get; private set; }
    public PolicyStatus Status { get; private set; }

    public PolicyDocument? PolicyDocument { get; private set; }
    public ICollection<Claim> Claims { get; private set; } = new List<Claim>();
    public ICollection<Renewal> Renewals { get; private set; } = new List<Renewal>();

    public void AttachDocument(PolicyDocument document)
    {
        PolicyDocument = document;
    }

    /// <summary>
    /// Olayın teminat DÖNEMİ içinde gerçekleşip gerçekleşmediği — sınırlar dahil: <c>StartDate ≤ olay ≤ EndDate</c>.
    /// StartDate/EndDate satın alma ANINI (saat dahil, UTC) taşır; bu nedenle kontrol saat hassasiyetlidir:
    /// aynı gün poliçe başlangıç saatinden SONRAKİ bir olay geçerli, ÖNCEKİ bir olay geçersizdir. Aktiflik
    /// (Status) ve "gelecekte olamaz" ayrı kurallardır; burada yalnızca teminat penceresi değerlendirilir.
    /// </summary>
    public bool CoversIncidentAt(DateTime incidentDate) =>
        incidentDate >= StartDate && incidentDate <= EndDate;

    /// <summary>Müşteri veya admin tarafından tetiklenen iptal; yalnızca aktif poliçe iptal edilebilir.</summary>
    public void Cancel()
    {
        if (Status != PolicyStatus.Active)
        {
            throw new DomainException("Yalnızca aktif poliçe iptal edilebilir.");
        }

        Status = PolicyStatus.Cancelled;
    }

    /// <summary>Arkaplan servisi bitiş tarihi geçen aktif poliçeleri bu metotla Expired durumuna çeker.</summary>
    public void ExpireIfPastEndDate(DateTime now)
    {
        if (Status != PolicyStatus.Active || now < EndDate)
        {
            return;
        }

        Status = PolicyStatus.Expired;
    }
}
