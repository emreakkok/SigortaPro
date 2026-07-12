using FluentValidation;

namespace SigortaPro.Application.Features.Claims.Commands.CreateClaim;

// Yapısal doğrulama (400). Poliçe durumu, olay tarihinin poliçe dönemi içinde ve gelecekte olmaması gibi
// bağlamsal iş kuralları poliçe + saat gerektirdiğinden handler'da uygulanır (BusinessRuleException → 409).
public sealed class CreateClaimCommandValidator : AbstractValidator<CreateClaimCommand>
{
    // Mock foto yükleme sınırları — gerçek depolama MVP dışı (PROJECT_CONTEXT §9); yalnızca metadata doğrulanır.
    private const int MaxPhotoCount = 10;
    private static readonly string[] AllowedPhotoExtensions = { ".jpg", ".jpeg", ".png", ".pdf" };

    public CreateClaimCommandValidator()
    {
        RuleFor(command => command.PolicyId)
            .NotEmpty().WithMessage("Poliçe kimliği zorunludur.");

        RuleFor(command => command.IncidentDate)
            .NotEmpty().WithMessage("Olay tarihi zorunludur.");

        RuleFor(command => command.Description)
            .NotEmpty().WithMessage("Hasar açıklaması zorunludur.")
            .MaximumLength(1000).WithMessage("Hasar açıklaması en fazla 1000 karakter olabilir.");

        RuleFor(command => command.EstimatedAmount)
            .GreaterThan(0).WithMessage("Tahmini hasar tutarı 0'dan büyük olmalıdır.");

        When(command => command.PhotoFileNames is not null, () =>
        {
            RuleFor(command => command.PhotoFileNames!)
                .Must(photos => photos.Count <= MaxPhotoCount)
                .WithMessage($"En fazla {MaxPhotoCount} foto yüklenebilir.");

            RuleForEach(command => command.PhotoFileNames!)
                .Must(HaveAllowedExtension)
                .WithMessage("Yalnızca .jpg, .jpeg, .png veya .pdf uzantılı foto yüklenebilir.");
        });
    }

    private static bool HaveAllowedExtension(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        return AllowedPhotoExtensions.Any(extension =>
            fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
    }
}
