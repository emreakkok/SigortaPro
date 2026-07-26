using FluentValidation;

namespace SigortaPro.Application.Features.Staff.Commands.UpdateStaffUser;

public sealed class UpdateStaffUserCommandValidator : AbstractValidator<UpdateStaffUserCommand>
{
    public UpdateStaffUserCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty().WithMessage("Personel kimliği zorunludur.");

        RuleFor(command => command.FullName)
            .NotEmpty().WithMessage("Ad soyad zorunludur.")
            .MinimumLength(2).WithMessage("Ad soyad en az 2 karakter olmalıdır.")
            .MaximumLength(100).WithMessage("Ad soyad en fazla 100 karakter olabilir.");
    }
}
