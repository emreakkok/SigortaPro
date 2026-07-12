using FluentValidation;
using SigortaPro.Application.Common.Validation;

namespace SigortaPro.Application.Features.Customers.Commands.UpdateProfile;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(command => command.FirstName)
            .NotEmpty().WithMessage("Ad zorunludur.")
            .MaximumLength(100).WithMessage("Ad en fazla 100 karakter olabilir.");

        RuleFor(command => command.LastName)
            .NotEmpty().WithMessage("Soyad zorunludur.")
            .MaximumLength(100).WithMessage("Soyad en fazla 100 karakter olabilir.");

        RuleFor(command => command.PhoneNumber)
            .NotEmpty().WithMessage("Telefon numarası zorunludur.")
            .Matches(ValidationPatterns.PhoneNumber).WithMessage("Telefon numarası +90 ile başlamalı ve 10 haneli olmalıdır.");

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
    }
}
