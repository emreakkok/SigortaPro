using FluentValidation;

namespace SigortaPro.Application.Features.Claims.Commands.ApproveClaim;

public sealed class ApproveClaimCommandValidator : AbstractValidator<ApproveClaimCommand>
{
    public ApproveClaimCommandValidator()
    {
        RuleFor(command => command.ClaimId)
            .NotEmpty().WithMessage("Hasar kimliği zorunludur.");

        RuleFor(command => command.ApprovedAmount)
            .GreaterThan(0).WithMessage("Onaylanan hasar tutarı 0'dan büyük olmalıdır.");

        RuleFor(command => command.ReviewNote)
            .MaximumLength(1000).WithMessage("Değerlendirme notu en fazla 1000 karakter olabilir.");
    }
}
