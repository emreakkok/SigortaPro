using FluentAssertions;
using SigortaPro.Application.Common.Pricing;

namespace SigortaPro.Application.Tests.Common;

// Hasar geçmişinin TEK ölçeği. Hesap durumsuzdur: hasarsız tamamlanan dönem +1, hasar −2,
// sonuç [−3, +6] aralığına sıkıştırılır. Dış (SigortaPro dışı) geçmiş varsayılmaz.
public class BonusMalusScaleTests
{
    [Fact]
    public void ComputeStep_Should_ReturnNeutral_ForNewCustomer()
    {
        // Geçmişi olmayan müşteri: ne indirim ne ceza. Dış geçmiş VARSAYILMAZ.
        BonusMalusScale.ComputeStep(claimFreeCompletedPeriods: 0, reportableClaims: 0)
            .Should().Be(BonusMalusScale.NeutralStep).And.Be(0);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 3)]
    [InlineData(6, 6)]
    public void ComputeStep_Should_RaiseOneStepPerClaimFreePeriod(int periods, int expected)
    {
        BonusMalusScale.ComputeStep(periods, reportableClaims: 0).Should().Be(expected);
    }

    [Theory]
    [InlineData(1, -2)]
    [InlineData(2, -3)] // −4 → tabana sıkışır
    public void ComputeStep_Should_DropTwoStepsPerClaim(int claims, int expected)
    {
        BonusMalusScale.ComputeStep(claimFreeCompletedPeriods: 0, reportableClaims: claims).Should().Be(expected);
    }

    [Fact]
    public void ComputeStep_Should_LetMalusDecay_AsClaimFreePeriodsAccumulate()
    {
        // Hasarlı müşteri kalıcı olarak cezalı KALMAZ; hasarsız dönemler basamağı geri yükseltir.
        var justAfterClaim = BonusMalusScale.ComputeStep(claimFreeCompletedPeriods: 0, reportableClaims: 1);
        var twoPeriodsLater = BonusMalusScale.ComputeStep(claimFreeCompletedPeriods: 2, reportableClaims: 1);
        var fourPeriodsLater = BonusMalusScale.ComputeStep(claimFreeCompletedPeriods: 4, reportableClaims: 1);

        justAfterClaim.Should().Be(-2);
        twoPeriodsLater.Should().Be(0, "iki hasarsız dönem sonrası nötre dönülür");
        fourPeriodsLater.Should().Be(2, "malus tamamen sönümlenip bonusa geçilir");
    }

    [Theory]
    [InlineData(100, 0, 6)]    // üst sınır
    [InlineData(0, 100, -3)]   // alt sınır
    public void ComputeStep_Should_ClampToScaleBounds(int periods, int claims, int expected)
    {
        BonusMalusScale.ComputeStep(periods, claims).Should().Be(expected);
        expected.Should().BeInRange(BonusMalusScale.MinStep, BonusMalusScale.MaxStep);
    }
}
