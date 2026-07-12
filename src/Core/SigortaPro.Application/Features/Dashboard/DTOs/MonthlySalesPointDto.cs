namespace SigortaPro.Application.Features.Dashboard.DTOs;

// Aylık satış trendi kalemi (poliçe oluşturulma ayına göre).
public sealed record MonthlySalesPointDto(
    int Year,
    int Month,
    int PolicyCount,
    decimal PremiumTotal);
