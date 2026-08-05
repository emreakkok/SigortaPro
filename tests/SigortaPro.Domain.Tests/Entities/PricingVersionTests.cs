using FluentAssertions;
using SigortaPro.Domain.Common;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Domain.Tests.Entities;

// ADR-048: Tarife versiyonu değişmezdir ve eksiksiz olmalıdır.
public class PricingVersionTests
{
    private static PricingVersion CreateVersion() =>
        new(1, "Test Tarifesi", new DateTime(2026, 7, 21, 0, 0, 0, DateTimeKind.Utc), "İlk tarife", Guid.NewGuid(), "Admin");

    [Fact]
    public void SetRate_Should_RejectNonPositiveBasePremium()
    {
        var version = CreateVersion();

        var act = () => version.SetRate(InsuranceBranch.Kasko, 0m);

        act.Should().Throw<DomainException>().WithMessage("*sıfırdan büyük*");
    }

    [Fact]
    public void SetRate_Should_RejectDuplicateBranch()
    {
        var version = CreateVersion();
        version.SetRate(InsuranceBranch.Kasko, 15000m);

        var act = () => version.SetRate(InsuranceBranch.Kasko, 16000m);

        act.Should().Throw<DomainException>().WithMessage("*zaten bir baz prim tanımlı*");
    }

    [Fact]
    public void EnsureCoversAllBranches_Should_Throw_When_TariffIsPartial()
    {
        // Kısmi tarife yayınlanırsa bazı branşlar sessizce baseline'a düşerdi → engellenir.
        var version = CreateVersion();
        version.SetRate(InsuranceBranch.Kasko, 15000m);

        var act = version.EnsureCoversAllBranches;

        act.Should().Throw<DomainException>().WithMessage("*tüm branşları içermelidir*");
    }

    [Fact]
    public void EnsureCoversAllBranches_Should_Pass_When_EveryBranchHasRate()
    {
        var version = CreateVersion();
        foreach (var branch in Enum.GetValues<InsuranceBranch>())
        {
            version.SetRate(branch, 1000m);
        }

        var act = version.EnsureCoversAllBranches;

        act.Should().NotThrow();
        version.Rates.Should().HaveCount(Enum.GetValues<InsuranceBranch>().Length);
    }

    [Fact]
    public void Constructor_Should_RejectNonPositiveVersionNumber()
    {
        var act = () => new PricingVersion(0, "Ad", DateTime.UtcNow, null, null, null);

        act.Should().Throw<DomainException>().WithMessage("*pozitif*");
    }

    // ── Yaşam döngüsü (Draft → Active → Archived) ──────────────────────────────────────────────
    private static PricingVersion FullDraft()
    {
        var version = CreateVersion();
        foreach (var branch in Enum.GetValues<InsuranceBranch>())
        {
            version.SetRate(branch, 5000m);
        }

        return version;
    }

    [Fact]
    public void NewVersion_Should_StartAsDraft()
    {
        CreateVersion().Status.Should().Be(PricingVersionStatus.Draft);
    }

    [Fact]
    public void Activate_Should_MoveDraftToActive_AndSetActivatedAt_PreservingEffectiveFrom()
    {
        var version = FullDraft();
        var effectiveFrom = version.EffectiveFrom;
        var now = new DateTime(2026, 8, 4, 10, 0, 0, DateTimeKind.Utc);

        version.Activate(now);

        version.Status.Should().Be(PricingVersionStatus.Active);
        version.ActivatedAt.Should().Be(now, "aktifleşme anı ActivatedAt'e yazılır");
        version.EffectiveFrom.Should().Be(effectiveFrom, "kullanıcının girdiği geçerlilik başlangıcı korunur");
    }

    [Fact]
    public void Activate_Should_Throw_When_TariffPartial()
    {
        var version = CreateVersion();
        version.SetRate(InsuranceBranch.Kasko, 15000m);

        var act = () => version.Activate(DateTime.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*tüm branşları içermelidir*");
    }

    [Fact]
    public void ActiveVersion_Should_BeImmutable_EditingThrows()
    {
        var version = FullDraft();
        version.Activate(DateTime.UtcNow);

        // Aktif versiyon değiştirilemez → geçmiş primler yapısal olarak korunur.
        version.Invoking(v => v.SetRate(InsuranceBranch.Konut, 9999m))
            .Should().Throw<DomainException>().WithMessage("*taslak*");
        version.Invoking(v => v.Activate(DateTime.UtcNow))
            .Should().Throw<DomainException>().WithMessage("*taslak*");
    }

    [Fact]
    public void Archive_Should_MoveActiveToArchived_And_RejectNonActive()
    {
        var draft = FullDraft();
        draft.Invoking(v => v.Archive()).Should().Throw<DomainException>("taslak arşivlenemez");

        draft.Activate(DateTime.UtcNow);
        draft.Archive();
        draft.Status.Should().Be(PricingVersionStatus.Archived);
    }

    [Fact]
    public void UpdateDraft_Should_ReplaceRatesAndRuleSet_WhileDraft()
    {
        var version = FullDraft();
        var ruleSet = new PricingRuleSet(
            new Dictionary<CoveragePackage, decimal> { [CoveragePackage.Standart] = 1.00m },
            new Dictionary<string, decimal> { ["İstanbul"] = 1.40m },
            1.00m,
            0.90m);
        var rates = Enum.GetValues<InsuranceBranch>().ToDictionary(branch => branch, _ => 7777m);

        version.UpdateDraft("Yeni Ad", new DateTime(2026, 8, 4, 0, 0, 0, DateTimeKind.Utc), null, "güncelleme", ruleSet, rates);

        version.Rates.Should().OnlyContain(rate => rate.BasePremium == 7777m);
        version.RuleSet!.RenewalDiscountFactor.Should().Be(0.90m);
        version.Note.Should().Be("güncelleme");
    }
}
