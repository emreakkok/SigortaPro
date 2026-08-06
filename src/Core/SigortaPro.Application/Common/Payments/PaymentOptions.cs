namespace SigortaPro.Application.Common.Payments;

// Ödeme akışının iş sabitleri. İzin verilen taksit sayıları hem FluentValidation kuralında hem de
// mock POS taksit hesaplayıcısında tek kaynaktan kullanılır (magic number yasağı).
public static class PaymentOptions
{
    public static readonly IReadOnlyList<int> AllowedInstallmentCounts = new[] { 1, 3, 6, 9, 12 };
}
