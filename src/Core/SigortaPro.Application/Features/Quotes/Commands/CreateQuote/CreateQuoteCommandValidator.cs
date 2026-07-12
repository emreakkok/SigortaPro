using FluentValidation;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Quotes.Commands.CreateQuote;

public sealed class CreateQuoteCommandValidator : AbstractValidator<CreateQuoteCommand>
{
    public CreateQuoteCommandValidator()
    {
        RuleFor(command => command.Branch)
            .IsInEnum().WithMessage("Geçersiz sigorta branşı.");

        RuleFor(command => command.CoveragePackage)
            .IsInEnum().WithMessage("Geçersiz teminat paketi.");

        // Kasko/Trafik → araç zorunlu; Konut/DASK → konut zorunlu; Sağlık → risk objesi gerekmez.
        When(command => command.Branch is InsuranceBranch.Kasko or InsuranceBranch.Trafik, () =>
        {
            RuleFor(command => command.VehicleId)
                .NotEmpty().WithMessage("Kasko/Trafik teklifi için araç seçimi zorunludur.");
        });

        When(command => command.Branch is InsuranceBranch.Konut or InsuranceBranch.Dask, () =>
        {
            RuleFor(command => command.PropertyId)
                .NotEmpty().WithMessage("Konut/DASK teklifi için konut seçimi zorunludur.");
        });
    }
}
