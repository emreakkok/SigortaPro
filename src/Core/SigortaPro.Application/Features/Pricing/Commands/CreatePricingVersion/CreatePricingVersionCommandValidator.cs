using FluentValidation;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Pricing.Commands.CreatePricingVersion;

// ADR-048 doğrulamaları. Kritik kural: **geçmişe tarihleme yasaktır** — tarife yalnızca "bundan sonra"
// geçerli olabilir. Böylece admin, geçmiş teklifleri etkileyebileceği yanılgısına düşmez.
public sealed class CreatePricingVersionCommandValidator : AbstractValidator<CreatePricingVersionCommand>
{
    // Üst sınır: veri girişi hatasına karşı emniyet supabı (ör. fazladan sıfır).
    private const decimal MaxBasePremium = 10_000_000m;

    // Geçmişe tarihleme toleransı: saat farkı/istek gecikmesi için küçük bir pay bırakılır.
    private static readonly TimeSpan BackdateTolerance = TimeSpan.FromMinutes(5);

    public CreatePricingVersionCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(command => command.EffectiveFrom)
            .Must(effectiveFrom => effectiveFrom >= dateTimeProvider.UtcNow - BackdateTolerance)
            .WithMessage(
                "Geçerlilik başlangıcı geçmiş bir tarih olamaz. Fiyat değişiklikleri yalnızca bundan sonra oluşturulacak teklifleri etkiler.");

        RuleFor(command => command.Note)
            .MaximumLength(300).WithMessage("Açıklama en fazla 300 karakter olabilir.");

        RuleFor(command => command.Rates)
            .NotEmpty().WithMessage("Tarife en az bir branş içermelidir.");

        RuleFor(command => command.Rates)
            .Must(CoversEveryBranchExactlyOnce)
            .WithMessage("Tarife tüm branşları tam olarak birer kez içermelidir.")
            .When(command => command.Rates is { Count: > 0 });

        RuleForEach(command => command.Rates).ChildRules(rate =>
        {
            rate.RuleFor(item => item.BasePremium)
                .GreaterThan(0m).WithMessage("Baz prim sıfırdan büyük olmalıdır.")
                .LessThanOrEqualTo(MaxBasePremium)
                .WithMessage($"Baz prim en fazla {MaxBasePremium:N0} olabilir.");

            rate.RuleFor(item => item.Branch)
                .IsInEnum().WithMessage("Geçersiz sigorta branşı.");
        });
    }

    private static bool CoversEveryBranchExactlyOnce(IReadOnlyList<BranchRateInput> rates)
    {
        var branches = rates.Select(rate => rate.Branch).ToList();
        if (branches.Count != branches.Distinct().Count())
        {
            return false;
        }

        return Enum.GetValues<InsuranceBranch>().All(branches.Contains);
    }
}
