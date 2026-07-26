using FluentValidation;

namespace SigortaPro.Application.Features.Customers.Commands.AddProperty;

public sealed class AddPropertyCommandValidator : AbstractValidator<AddPropertyCommand>
{
    private const int MaxBuildingAge = 200;
    private const int MaxSquareMeters = 10000;

    // Türkiye deprem bölgesi haritası: 1 (en yüksek risk) – 5 (en düşük risk).

    public AddPropertyCommandValidator()
    {
        RuleFor(command => command.City)
            .NotEmpty().WithMessage("İl zorunludur.")
            .MaximumLength(100).WithMessage("İl en fazla 100 karakter olabilir.");

        RuleFor(command => command.District)
            .NotEmpty().WithMessage("İlçe zorunludur.")
            .MaximumLength(100).WithMessage("İlçe en fazla 100 karakter olabilir.");

        RuleFor(command => command.Neighborhood)
            .NotEmpty().WithMessage("Mahalle zorunludur.")
            .MaximumLength(150).WithMessage("Mahalle en fazla 150 karakter olabilir.");

        RuleFor(command => command.PostalCode)
            .NotEmpty().WithMessage("Posta kodu zorunludur.")
            .MaximumLength(10).WithMessage("Posta kodu en fazla 10 karakter olabilir.");

        RuleFor(command => command.BuildingAge)
            .InclusiveBetween(0, MaxBuildingAge)
            .WithMessage($"Bina yaşı 0 ile {MaxBuildingAge} arasında olmalıdır.");

        RuleFor(command => command.SquareMeters)
            .InclusiveBetween(1, MaxSquareMeters)
            .WithMessage($"Metrekare 1 ile {MaxSquareMeters} arasında olmalıdır.");

    }
}
