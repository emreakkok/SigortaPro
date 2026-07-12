using FluentValidation;

namespace SigortaPro.Application.Features.Payments.Queries.GetInstallmentOptions;

public sealed class GetInstallmentOptionsQueryValidator : AbstractValidator<GetInstallmentOptionsQuery>
{
    public GetInstallmentOptionsQueryValidator()
    {
        RuleFor(query => query.QuoteId)
            .NotEmpty().WithMessage("Teklif kimliği zorunludur.");
    }
}
