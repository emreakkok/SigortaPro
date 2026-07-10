using SigortaPro.Domain.Common;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Domain.Entities;

public class Claim : BaseEntity, IAggregateRoot
{
    protected Claim()
    {
    }

    public Claim(Guid policyId, Guid customerId, DateTime incidentDate, string description, decimal estimatedAmount)
    {
        Id = Guid.NewGuid();
        PolicyId = policyId;
        CustomerId = customerId;
        IncidentDate = incidentDate;
        Description = description;
        EstimatedAmount = estimatedAmount;
        Status = ClaimStatus.Submitted;
    }

    public Guid PolicyId { get; private set; }
    public Policy? Policy { get; private set; }
    public Guid CustomerId { get; private set; }
    public Customer? Customer { get; private set; }
    public DateTime IncidentDate { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal EstimatedAmount { get; private set; }
    public decimal? ApprovedAmount { get; private set; }
    public ClaimStatus Status { get; private set; }
    public string? ReviewNote { get; private set; }

    /// <summary>Submitted → UnderReview. Admin incelemeyi başlattığında çağrılır.</summary>
    public void StartReview()
    {
        if (Status != ClaimStatus.Submitted)
        {
            throw new DomainException("Yalnızca bildirilmiş hasar incelemeye alınabilir.");
        }

        Status = ClaimStatus.UnderReview;
    }

    /// <summary>UnderReview → Approved.</summary>
    public void Approve(decimal approvedAmount, string? reviewNote)
    {
        if (Status != ClaimStatus.UnderReview)
        {
            throw new DomainException("Yalnızca incelemedeki hasar onaylanabilir.");
        }

        ApprovedAmount = approvedAmount;
        ReviewNote = reviewNote;
        Status = ClaimStatus.Approved;
    }

    /// <summary>UnderReview → Rejected.</summary>
    public void Reject(string reviewNote)
    {
        if (Status != ClaimStatus.UnderReview)
        {
            throw new DomainException("Yalnızca incelemedeki hasar reddedilebilir.");
        }

        ReviewNote = reviewNote;
        Status = ClaimStatus.Rejected;
    }

    /// <summary>Approved → Paid. Hasar ödemesi yapıldığında çağrılır.</summary>
    public void MarkPaid()
    {
        if (Status != ClaimStatus.Approved)
        {
            throw new DomainException("Yalnızca onaylanmış hasar ödenmiş olarak işaretlenebilir.");
        }

        Status = ClaimStatus.Paid;
    }
}
