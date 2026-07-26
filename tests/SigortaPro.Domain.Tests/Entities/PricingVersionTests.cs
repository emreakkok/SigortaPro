using FluentAssertions;
using SigortaPro.Domain.Common;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Domain.Tests.Entities;

// ADR-048: Tarife versiyonu değişmezdir ve eksiksiz olmalıdır.
public class PricingVersionTests
{
    private static PricingVersion CreateVersion() =>
        new(1, new DateTime(2026, 7, 21, 0, 0, 0, DateTimeKind.Utc), "İlk tarife", Guid.NewGuid(), "Admin");

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
        var act = () => new PricingVersion(0, DateTime.UtcNow, null, null, null);

        act.Should().Throw<DomainException>().WithMessage("*pozitif*");
    }
}
