using SigortaPro.Application.Features.Payments.Commands.PurchaseQuote;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Tests.Features.Payments;

// Ödeme modülü testlerinde kullanılan entity/komut kurucuları.
internal static class PaymentTestData
{
    // Luhn-geçerli örnek başarı kartı (README test kartı).
    public const string ValidCardNumber = "4111111111111111";

    public static Quote ApprovedQuote(Guid customerId, decimal premium, DateTime validUntil)
    {
        var quote = new Quote(customerId, Guid.NewGuid(), InsuranceBranch.Kasko, Guid.NewGuid(), null);
        quote.MarkAsPriced(premium, validUntil);
        quote.Approve();
        return quote;
    }

    public static PurchaseQuoteCommand PurchaseCommand(Guid quoteId, string cardNumber = ValidCardNumber, int installmentCount = 1) =>
        new(
            QuoteId: quoteId,
            CardNumber: cardNumber,
            CardHolderName: "Ayşe Yılmaz",
            ExpiryMonth: "12",
            ExpiryYear: "2030",
            Cvv: "123",
            InstallmentCount: installmentCount);
}
