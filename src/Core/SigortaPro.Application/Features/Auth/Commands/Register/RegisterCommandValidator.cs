using FluentValidation;
using SigortaPro.Application.Common.Validation;

namespace SigortaPro.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    // Türk plaka değil; telefon formatı CODING_STANDARDS.md §6.2: +90 prefiksi + 10 hane.
    private const string PhoneRegex = @"^\+90\d{10}$";

    public RegisterCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("E-posta adresi zorunludur.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
            .MaximumLength(256).WithMessage("E-posta adresi en fazla 256 karakter olabilir.");

        // Identity varsayılan şifre politikasıyla uyumlu (büyük/küçük harf, rakam, özel karakter).
        RuleFor(command => command.Password)
            .NotEmpty().WithMessage("Şifre zorunludur.")
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır.")
            .Matches("[A-Z]").WithMessage("Şifre en az bir büyük harf içermelidir.")
            .Matches("[a-z]").WithMessage("Şifre en az bir küçük harf içermelidir.")
            .Matches("[0-9]").WithMessage("Şifre en az bir rakam içermelidir.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Şifre en az bir özel karakter içermelidir.");

        RuleFor(command => command.FirstName)
            .NotEmpty().WithMessage("Ad zorunludur.")
            .MaximumLength(100).WithMessage("Ad en fazla 100 karakter olabilir.");

        RuleFor(command => command.LastName)
            .NotEmpty().WithMessage("Soyad zorunludur.")
            .MaximumLength(100).WithMessage("Soyad en fazla 100 karakter olabilir.");

        RuleFor(command => command.Tckn)
            .NotEmpty().WithMessage("TCKN zorunludur.")
            .Must(TcknValidation.IsValid).WithMessage("Geçerli bir TCKN giriniz.");

        RuleFor(command => command.BirthDate)
            .NotEmpty().WithMessage("Doğum tarihi zorunludur.")
            .LessThan(DateTime.UtcNow).WithMessage("Doğum tarihi bugünden sonra olamaz.")
            .Must(BeAtLeast18YearsOld).WithMessage("Sigortalı en az 18 yaşında olmalıdır.");

        RuleFor(command => command.PhoneNumber)
            .NotEmpty().WithMessage("Telefon numarası zorunludur.")
            .Matches(PhoneRegex).WithMessage("Telefon numarası +90 ile başlamalı ve 10 haneli olmalıdır.");

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

    private static bool BeAtLeast18YearsOld(DateTime birthDate)
    {
        var today = DateTime.UtcNow.Date;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age))
        {
            age--;
        }

        return age >= 18;
    }
}
