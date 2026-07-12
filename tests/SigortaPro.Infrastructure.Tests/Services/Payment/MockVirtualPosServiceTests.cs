using FluentAssertions;
using SigortaPro.Application.Common.Payments;
using SigortaPro.Infrastructure.Services.Payment;

namespace SigortaPro.Infrastructure.Tests.Services.Payment;

public class MockVirtualPosServiceTests
{
    private readonly MockVirtualPosService _service = new();

    private static PaymentChargeRequest Charge(string cardNumber, int installmentCount = 1) => new(
        CardNumber: cardNumber,
        CardHolderName: "Ayşe Yılmaz",
        ExpiryMonth: "12",
        ExpiryYear: "2030",
        Cvv: "123",
        Amount: 12000m,
        InstallmentCount: installmentCount);

    [Fact]
    public async Task ChargeAsync_Should_Succeed_When_CardIsLuhnValidAndNotAScenarioCard()
    {
        var result = await _service.ChargeAsync(Charge("4111111111111111"));

        result.IsSuccess.Should().BeTrue();
        result.ProviderReferenceCode.Should().NotBeNullOrWhiteSpace();
        result.MaskedCardNumber.Should().Be("************1111");
        result.FailureReason.Should().BeNull();
    }

    [Fact]
    public async Task ChargeAsync_Should_Fail_When_CardFailsLuhnCheck()
    {
        // Son hanesi bozularak Luhn geçersiz kılınmış numara.
        var result = await _service.ChargeAsync(Charge("4111111111111112"));

        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Be("Geçersiz kart numarası.");
    }

    [Fact]
    public async Task ChargeAsync_Should_Fail_WithInsufficientFunds_When_ScenarioCardUsed()
    {
        var result = await _service.ChargeAsync(Charge("4000000000000002"));

        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Be("Yetersiz bakiye.");
    }

    [Fact]
    public async Task ChargeAsync_Should_Fail_With3DSecureError_When_ScenarioCardUsed()
    {
        var result = await _service.ChargeAsync(Charge("4000000000000069"));

        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Be("3D Secure doğrulaması başarısız.");
    }

    [Fact]
    public async Task ChargeAsync_Should_StripSeparatorsAndSucceed_When_CardHasSpaces()
    {
        var result = await _service.ChargeAsync(Charge("4111 1111 1111 1111"));

        result.IsSuccess.Should().BeTrue();
        result.MaskedCardNumber.Should().Be("************1111");
    }

    [Fact]
    public void GetInstallmentOptions_Should_ReturnInterestFreePlans_ForAllowedCounts()
    {
        var options = _service.GetInstallmentOptions(12000m);

        options.Select(option => option.Count).Should().Equal(PaymentOptions.AllowedInstallmentCounts);
        // Faizsiz: her planın toplamı tutara eşit.
        options.Should().OnlyContain(option => option.TotalAmount == 12000m);
        options.Single(option => option.Count == 3).MonthlyAmount.Should().Be(4000m);
    }
}
