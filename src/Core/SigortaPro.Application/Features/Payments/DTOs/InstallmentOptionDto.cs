namespace SigortaPro.Application.Features.Payments.DTOs;

// Ödeme sayfasında gösterilecek taksit seçeneği (faizsiz mock; TotalAmount == prim).
public sealed record InstallmentOptionDto(
    int Count,
    decimal MonthlyAmount,
    decimal TotalAmount);
