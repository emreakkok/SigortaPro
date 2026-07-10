using SigortaPro.Domain.Common;

namespace SigortaPro.Domain.Entities;

public class Renewal : BaseEntity, IAggregateRoot
{
    protected Renewal()
    {
    }

    public Renewal(Guid policyId, Guid newQuoteId, DateTime offeredAt)
    {
        Id = Guid.NewGuid();
        PolicyId = policyId;
        NewQuoteId = newQuoteId;
        OfferedAt = offeredAt;
    }

    public Guid PolicyId { get; private set; }
    public Policy? Policy { get; private set; }
    public Guid NewQuoteId { get; private set; }
    public Quote? NewQuote { get; private set; }
    public DateTime OfferedAt { get; private set; }
    public bool IsAccepted { get; private set; }
    public DateTime? AcceptedAt { get; private set; }

    /// <summary>Müşteri yenileme teklifini onayladığında çağrılır.</summary>
    public void Accept(DateTime acceptedAt)
    {
        if (IsAccepted)
        {
            throw new DomainException("Yenileme teklifi zaten onaylanmış.");
        }

        IsAccepted = true;
        AcceptedAt = acceptedAt;
    }
}
