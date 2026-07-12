using FluentValidation;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Quotes.Queries.GetQuoteComparison;

public sealed class GetQuoteComparisonQueryValidator : AbstractValidator<GetQuoteComparisonQuery>
{
    public GetQuoteComparisonQueryValidator()
    {
        RuleFor(query => query.Branch)
            .IsInEnum().WithMessage("Geçersiz sigorta branşı.");

        When(query => query.Branch is InsuranceBranch.Kasko or InsuranceBranch.Trafik, () =>
        {
            RuleFor(query => query.VehicleId)
                .NotEmpty().WithMessage("Kasko/Trafik karşılaştırması için araç seçimi zorunludur.");
        });

        When(query => query.Branch is InsuranceBranch.Konut or InsuranceBranch.Dask, () =>
        {
            RuleFor(query => query.PropertyId)
                .NotEmpty().WithMessage("Konut/DASK karşılaştırması için konut seçimi zorunludur.");
        });
    }
}
