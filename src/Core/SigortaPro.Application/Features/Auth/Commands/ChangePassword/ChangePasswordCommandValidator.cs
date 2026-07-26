using FluentValidation;

namespace SigortaPro.Application.Features.Auth.Commands.ChangePassword;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(command => command.CurrentPassword)
            .NotEmpty().WithMessage("Mevcut şifre zorunludur.");

        // Şifre politikası Register/ResetPassword validator'larıyla birebir aynı (Identity varsayılanıyla uyumlu).
        RuleFor(command => command.NewPassword)
            .NotEmpty().WithMessage("Şifre zorunludur.")
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır.")
            .Matches("[A-Z]").WithMessage("Şifre en az bir büyük harf içermelidir.")
            .Matches("[a-z]").WithMessage("Şifre en az bir küçük harf içermelidir.")
            .Matches("[0-9]").WithMessage("Şifre en az bir rakam içermelidir.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Şifre en az bir özel karakter içermelidir.")
            .NotEqual(command => command.CurrentPassword)
            .WithMessage("Yeni şifre mevcut şifreyle aynı olamaz.");
    }
}
