using FluentValidation;
using SigortaPro.Application.Common.Validation;

namespace SigortaPro.Application.Features.Customers.Commands.UpdateVehicle;

public sealed class UpdateVehicleCommandValidator : AbstractValidator<UpdateVehicleCommand>
{
    private const int MinManufactureYear = 1950;
    private const int MaxEnginePowerHp = 2000;

    public UpdateVehicleCommandValidator()
    {
        RuleFor(command => command.VehicleId)
            .NotEmpty().WithMessage("Araç kimliği zorunludur.");

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

        // ADR-057: Kullanım amacı beyanı ZORUNLUDUR ve varsayılan atanmaz — prim doğrudan etkilendiğinden
        // kullanıcı bilinçli seçim yapmalıdır (sessiz varsayım yasak).
        RuleFor(command => command.UsagePurpose)
            .NotNull().WithMessage("Aracın kullanım amacı seçilmelidir.")
            .IsInEnum().WithMessage("Geçersiz kullanım amacı.");
    }
}
