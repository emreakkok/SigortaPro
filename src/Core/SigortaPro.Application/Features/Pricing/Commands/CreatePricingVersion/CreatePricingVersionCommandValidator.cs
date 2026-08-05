using FluentValidation;

namespace SigortaPro.Application.Features.Pricing.Commands.CreatePricingVersion;

public sealed class CreatePricingVersionCommandValidator : AbstractValidator<CreatePricingVersionCommand>
{
    public CreatePricingVersionCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Taslak adı zorunludur.")
            .MaximumLength(120).WithMessage("Taslak adı en fazla 120 karakter olabilir.");
    }
}
