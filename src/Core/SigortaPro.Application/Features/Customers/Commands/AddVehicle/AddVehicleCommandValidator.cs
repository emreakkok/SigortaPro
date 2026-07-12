using FluentValidation;
using SigortaPro.Application.Common.Validation;

namespace SigortaPro.Application.Features.Customers.Commands.AddVehicle;

public sealed class AddVehicleCommandValidator : AbstractValidator<AddVehicleCommand>
{
    // Motorlu taşıtlar için makul üretim yılı alt sınırı; üst sınır cari yıl + 1 (model yılı).
    private const int MinManufactureYear = 1950;

    // Aşırı/anlamsız motor gücü değerlerini eler (birim: beygir gücü).
    private const int MaxEnginePowerHp = 2000;

    public AddVehicleCommandValidator()
    {
        RuleFor(command => command.PlateNumber)
            .NotEmpty().WithMessage("Plaka zorunludur.")
            .Matches(ValidationPatterns.TurkishPlate).WithMessage("Geçerli bir Türk plakası giriniz (örn. 34 ABC 123).");

        RuleFor(command => command.Brand)
            .NotEmpty().WithMessage("Marka zorunludur.")
            .MaximumLength(100).WithMessage("Marka en fazla 100 karakter olabilir.");

        RuleFor(command => command.Model)
            .NotEmpty().WithMessage("Model zorunludur.")
            .MaximumLength(100).WithMessage("Model en fazla 100 karakter olabilir.");

        RuleFor(command => command.ManufactureYear)
            .InclusiveBetween(MinManufactureYear, DateTime.UtcNow.Year + 1)
            .WithMessage($"Üretim yılı {MinManufactureYear} ile {DateTime.UtcNow.Year + 1} arasında olmalıdır.");

        RuleFor(command => command.EnginePowerHp)
            .InclusiveBetween(1, MaxEnginePowerHp)
            .WithMessage($"Motor gücü 1 ile {MaxEnginePowerHp} beygir arasında olmalıdır.");
    }
}
