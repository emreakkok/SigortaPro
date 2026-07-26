using FluentAssertions;
using SigortaPro.Application.Features.Renewals;

namespace SigortaPro.Application.Tests.Features.Renewals;

// Yenileme teklifi geçerlilik tarihi: müşteri poliçesi bitene kadar kabul edebilmelidir → geçerlilik
// poliçe bitişine kadardır. Örnek: poliçe 27.07.2026 bitiyor, yenileme 12.07.2026'da üretiliyor →
// teklif poliçe hâlâ aktifken (üretim + 7 = 19.07) DEĞİL, poliçe bitişinde (27.07) sona ermelidir.
public class RenewalOfferValidityTests
{
    private static readonly DateTime GeneratedAt = new(2026, 7, 12, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Compute_Should_UsePolicyEndDate_When_PolicyEndsAfterStandardWindow()
    {
        var policyEnd = new DateTime(2026, 7, 27, 0, 0, 0, DateTimeKind.Utc); // üretimden 15 gün sonra (> 7)

        var validUntil = RenewalOfferValidity.Compute(GeneratedAt, policyEnd);

        validUntil.Should().Be(policyEnd,
            "yenileme teklifi, poliçe hâlâ aktifken süresi dolmamalı → poliçe bitişine (27.07) kadar geçerli");
        validUntil.Should().NotBe(GeneratedAt.AddDays(7), "eski hatalı davranış (üretim + 7 = 19.07) olmamalı");
    }

    [Fact]
    public void Compute_Should_GuaranteeMinimumWindow_When_PolicyEndsSoon()
    {
        // Poliçe bitişine 3 gün kala üretim → en az standart pencere (7 gün) garanti edilir.
        var policyEnd = GeneratedAt.AddDays(3);

        var validUntil = RenewalOfferValidity.Compute(GeneratedAt, policyEnd);

        validUntil.Should().Be(GeneratedAt.AddDays(7));
    }
}
