using FluentValidation;

namespace SigortaPro.Application.Features.Claims.Commands.CreateClaim;

// Yapısal doğrulama (400). Poliçe durumu, olay tarihinin poliçe dönemi içinde ve gelecekte olmaması gibi
// bağlamsal iş kuralları poliçe + saat gerektirdiğinden handler'da uygulanır (BusinessRuleException → 409).
public sealed class CreateClaimCommandValidator : AbstractValidator<CreateClaimCommand>
{
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

        // Belge (foto/PDF) yükleme sınırları: adet, tür ve boyut. Baytlar depolamaya yazılır (ADR-023).
        When(command => command.Documents is not null, () =>
        {
            RuleFor(command => command.Documents!)
                .Must(documents => documents.Count <= ClaimDocumentStorage.MaxDocumentCount)
                .WithMessage($"En fazla {ClaimDocumentStorage.MaxDocumentCount} belge yüklenebilir.");

            RuleForEach(command => command.Documents!).ChildRules(document =>
            {
                document.RuleFor(item => item.FileName)
                    .NotEmpty().WithMessage("Belge adı zorunludur.")
                    .MaximumLength(260).WithMessage("Belge adı en fazla 260 karakter olabilir.");

                document.RuleFor(item => item.ContentType)
                    .Must(contentType => ClaimDocumentStorage.AllowedContentTypes.Contains(contentType))
                    .WithMessage("Yalnızca JPEG, PNG, WEBP görsel veya PDF belge yüklenebilir.");

                document.RuleFor(item => item.Content)
                    .NotNull().WithMessage("Belge içeriği zorunludur.")
                    .Must(content => content is { Length: > 0 })
                    .WithMessage("Boş belge yüklenemez.")
                    .Must(content => content is null || content.LongLength <= ClaimDocumentStorage.MaxDocumentSizeBytes)
                    .WithMessage("Her belge en fazla 3 MB olabilir.");
            });
        });
    }
}
