using FluentValidation;

namespace SigortaPro.Application.Features.Staff.Commands.CreateStaffUser;

public sealed class CreateStaffUserCommandValidator : AbstractValidator<CreateStaffUserCommand>
{
    public CreateStaffUserCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("E-posta adresi zorunludur.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
            .MaximumLength(256).WithMessage("E-posta adresi en fazla 256 karakter olabilir.");

        RuleFor(command => command.FullName)
            .NotEmpty().WithMessage("Ad soyad zorunludur.")
            .MinimumLength(2).WithMessage("Ad soyad en az 2 karakter olmalıdır.")
            .MaximumLength(100).WithMessage("Ad soyad en fazla 100 karakter olabilir.");

        // Kayıt (RegisterCommand) ile aynı Identity şifre politikası — tutarlılık için birebir.
        RuleFor(command => command.Password)
            .NotEmpty().WithMessage("Şifre zorunludur.")
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır.")
            .Matches("[A-Z]").WithMessage("Şifre en az bir büyük harf içermelidir.")
            .Matches("[a-z]").WithMessage("Şifre en az bir küçük harf içermelidir.")
            .Matches("[0-9]").WithMessage("Şifre en az bir rakam içermelidir.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Şifre en az bir özel karakter içermelidir.");
    }
}
