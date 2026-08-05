using System.Linq.Expressions;
using FluentValidation;

namespace SigortaPro.Application.Features.Pricing.Commands.UpdatePricingDraft;

// Tarife girdileri pozitif olmalı; kısmi/eksik değerler sessizce baseline'a düşmemeli (fiyat boşluğu riski).
// Branşların eksiksizliği domain'de (EnsureCoversAllBranches) kesin olarak kontrol edilir.
public sealed class UpdatePricingDraftCommandValidator : AbstractValidator<UpdatePricingDraftCommand>
{
    public UpdatePricingDraftCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Taslak adı zorunludur.")
            .MaximumLength(120).WithMessage("Taslak adı en fazla 120 karakter olabilir.");

        // Bitiş verilmişse başlangıçtan sonra olmalı (son sözü domain de söyler).
        RuleFor(command => command.EffectiveTo)
            .Must((command, effectiveTo) => effectiveTo is null || effectiveTo > command.EffectiveFrom)
            .WithMessage("Geçerlilik bitişi, başlangıcından sonra olmalıdır.");

        RuleFor(command => command.Rates)
            .NotEmpty().WithMessage("Baz prim listesi boş olamaz.");

        // Bantlı faktör listeleri tam uzunlukta ve pozitif olmalı → indeks sözleşmesi bozulmasın (baseline'a düşmesin).
        BandFactor(command => command.DriverAgeFactors, 3, "Sürücü yaşı");
        BandFactor(command => command.VehicleAgeFactors, 3, "Araç yaşı");
        BandFactor(command => command.EnginePowerFactors, 4, "Motor gücü");
        BandFactor(command => command.VehicleUsageFactors, 3, "Kullanım amacı");
        BandFactor(command => command.BonusMalusFactors, 10, "Hasarsızlık basamağı");
        BandFactor(command => command.BuildingAgeFactors, 4, "Bina yaşı");
        BandFactor(command => command.SquareMetersFactors, 4, "Metrekare");
        BandFactor(command => command.EarthquakeZoneFactors, 6, "Deprem bölgesi");
        BandFactor(command => command.HealthAgeFactors, 5, "Sağlık yaş bandı");

        RuleFor(command => command.SmokerSurcharge)
            .GreaterThan(0m).WithMessage("Sigara ek prim çarpanı sıfırdan büyük olmalıdır.");

        RuleForEach(command => command.Rates).ChildRules(rate =>
        {
            rate.RuleFor(item => item.Branch).IsInEnum().WithMessage("Geçersiz sigorta branşı.");
            rate.RuleFor(item => item.BasePremium)
                .GreaterThan(0m).WithMessage("Baz prim sıfırdan büyük olmalıdır.");
        });

        RuleForEach(command => command.PackagePremiumFactors).ChildRules(factor =>
        {
            factor.RuleFor(item => item.Package).IsInEnum().WithMessage("Geçersiz teminat paketi.");
            factor.RuleFor(item => item.PremiumFactor)
                .GreaterThan(0m).WithMessage("Paket çarpanı sıfırdan büyük olmalıdır.");
        });

        RuleForEach(command => command.CityRiskCoefficients).ChildRules(city =>
        {
            city.RuleFor(item => item.City)
                .NotEmpty().WithMessage("İl adı zorunludur.")
                .MaximumLength(100).WithMessage("İl adı en fazla 100 karakter olabilir.");
            city.RuleFor(item => item.Coefficient)
                .GreaterThan(0m).WithMessage("İl risk katsayısı sıfırdan büyük olmalıdır.");
        });

        RuleFor(command => command.DefaultCityRiskCoefficient)
            .GreaterThan(0m).WithMessage("Varsayılan il risk katsayısı sıfırdan büyük olmalıdır.");

        // Yenileme indirimi bir İNDİRİMDİR: (0, 1]. 1.00 = indirim yok.
        RuleFor(command => command.RenewalDiscountFactor)
            .GreaterThan(0m).WithMessage("Yenileme indirim çarpanı sıfırdan büyük olmalıdır.")
            .LessThanOrEqualTo(1.00m).WithMessage("Yenileme indirim çarpanı en fazla 1.00 olabilir (indirim yok = 1.00).");
    }

    // Bantlı bir faktör listesi: tam beklenen uzunlukta olmalı ve tüm çarpanlar pozitif olmalı.
    private void BandFactor(
        Expression<Func<UpdatePricingDraftCommand, IReadOnlyList<decimal>>> selector, int expectedCount, string label)
    {
        RuleFor(selector)
            .Must(list => list is not null && list.Count == expectedCount && list.All(value => value > 0m))
            .WithMessage($"{label} faktörleri {expectedCount} pozitif değer içermelidir.");
    }
}
