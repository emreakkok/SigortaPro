namespace SigortaPro.Application.Common.Payments;

// Bir ödeme tutarı için taksit seçeneği (mock POS faizsiz taksit varsayar: TotalAmount == tutar).
public sealed record InstallmentOption(
    int Count,
    decimal MonthlyAmount,
    decimal TotalAmount);
