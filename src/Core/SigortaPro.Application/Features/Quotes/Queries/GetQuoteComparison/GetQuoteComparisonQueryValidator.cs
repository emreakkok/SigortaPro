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

        // ADR-056: Kurallar teklif OLUŞTURMA ile birebir aynıdır. Aksi hâlde önizleme, beyanı olmayan bir
        // girdiyle fiyatlanır ve gösterilen prim oluşacak teklifin priminden sapardı.
        When(query => query.Branch == InsuranceBranch.Saglik, () =>
        {
            RuleFor(query => query.IsSmoker)
                .NotNull().WithMessage("Sağlık karşılaştırması için sigara kullanım beyanı zorunludur.");
        });

        When(query => query.Branch != InsuranceBranch.Saglik, () =>
        {
            RuleFor(query => query.IsSmoker)
                .Null().WithMessage("Sigara beyanı yalnızca Sağlık branşında gönderilebilir.");
        });
    }
}
