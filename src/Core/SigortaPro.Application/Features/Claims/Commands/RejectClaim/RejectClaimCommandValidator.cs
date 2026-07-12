using FluentValidation;

namespace SigortaPro.Application.Features.Claims.Commands.RejectClaim;

public sealed class RejectClaimCommandValidator : AbstractValidator<RejectClaimCommand>
{
    public RejectClaimCommandValidator()
    {
        RuleFor(command => command.ClaimId)
            .NotEmpty().WithMessage("Hasar kimliği zorunludur.");

        RuleFor(command => command.ReviewNote)
            .NotEmpty().WithMessage("Ret gerekçesi (değerlendirme notu) zorunludur.")
            .MaximumLength(1000).WithMessage("Değerlendirme notu en fazla 1000 karakter olabilir.");
    }
}
