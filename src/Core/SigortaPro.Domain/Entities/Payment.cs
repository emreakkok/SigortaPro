using SigortaPro.Domain.Common;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Domain.Entities;

public class Payment : BaseEntity, IAggregateRoot
{
    protected Payment()
    {
    }

    public Payment(Guid customerId, Guid quoteId, decimal amount, int installmentCount, string maskedCardNumber, DateTime transactionDate)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        QuoteId = quoteId;
        Amount = amount;
        InstallmentCount = installmentCount;
        MaskedCardNumber = maskedCardNumber;
        TransactionDate = transactionDate;
        Status = PaymentStatus.Pending;
    }

    public Guid CustomerId { get; private set; }
    public Customer? Customer { get; private set; }
    public Guid QuoteId { get; private set; }
    public Quote? Quote { get; private set; }
    public decimal Amount { get; private set; }
    public int InstallmentCount { get; private set; }
    public string MaskedCardNumber { get; private set; } = string.Empty;
    public PaymentStatus Status { get; private set; }
    public DateTime TransactionDate { get; private set; }
    public string? ProviderReferenceCode { get; private set; }
    public string? FailureReason { get; private set; }

    public void MarkSuccessful(string? providerReferenceCode)
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new DomainException("Yalnızca bekleyen ödeme başarılı olarak işaretlenebilir.");
        }

        Status = PaymentStatus.Successful;
        ProviderReferenceCode = providerReferenceCode;
    }

    public void MarkFailed(string failureReason)
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new DomainException("Yalnızca bekleyen ödeme başarısız olarak işaretlenebilir.");
        }

        Status = PaymentStatus.Failed;
        FailureReason = failureReason;
    }
}
