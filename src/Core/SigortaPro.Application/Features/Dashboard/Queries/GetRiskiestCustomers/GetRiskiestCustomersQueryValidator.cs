using FluentValidation;

namespace SigortaPro.Application.Features.Dashboard.Queries.GetRiskiestCustomers;

public sealed class GetRiskiestCustomersQueryValidator : AbstractValidator<GetRiskiestCustomersQuery>
{
    // Üst sınır: dashboard segment listesi sınırlı tutulur (aşırı büyük istekleri engeller).
    private const int MaxTop = 50;

    public GetRiskiestCustomersQueryValidator()
    {
        RuleFor(query => query.Top)
            .InclusiveBetween(1, MaxTop)
            .WithMessage($"Segment sayısı 1 ile {MaxTop} arasında olmalıdır.");
    }
}
