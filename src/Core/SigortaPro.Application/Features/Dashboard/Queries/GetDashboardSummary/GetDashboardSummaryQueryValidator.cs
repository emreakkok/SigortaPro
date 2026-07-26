using FluentValidation;

namespace SigortaPro.Application.Features.Dashboard.Queries.GetDashboardSummary;

// Yapısal doğrulama (400): aralık tutarlı ve makul uzunlukta olmalı. İkisi de verilmezse varsayılan
// (son 30 gün) handler'da uygulanır; bu yüzden kurallar yalnızca DEĞER VERİLDİĞİNDE çalışır.
public sealed class GetDashboardSummaryQueryValidator : AbstractValidator<GetDashboardSummaryQuery>
{
    // Çok uzun aralıklar aylık kovaya düşse de sorguyu ağırlaştırır; 5 yıl fazlasıyla yeterli.
    private const int MaxRangeDays = 366 * 5;

    public GetDashboardSummaryQueryValidator()
    {
        RuleFor(query => query)
            .Must(query => query.From is null || query.To is null || query.From <= query.To)
            .WithMessage("Bitiş tarihi başlangıç tarihinden önce olamaz.");

        RuleFor(query => query)
            .Must(query => query.From is null || query.To is null
                || (query.To.Value - query.From.Value).TotalDays <= MaxRangeDays)
            .WithMessage($"Tarih aralığı en fazla {MaxRangeDays} gün olabilir.");
    }
}
