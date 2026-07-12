using FluentAssertions;
using SigortaPro.Application.Features.Payments.Commands.PurchaseQuote;

namespace SigortaPro.Application.Tests.Features.Payments;

public class PurchaseQuoteCommandValidatorTests
{
    private readonly PurchaseQuoteCommandValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_When_CommandIsWellFormed()
    {
        var command = PaymentTestData.PurchaseCommand(Guid.NewGuid());

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_Pass_When_CardNumberHasSpaces()
    {
        var command = PaymentTestData.PurchaseCommand(Guid.NewGuid(), cardNumber: "4111 1111 1111 1111");

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("411111111111")]        // 12 hane — çok kısa
    [InlineData("41111111111111111111")] // 20 hane — çok uzun
    [InlineData("4111-1111-1111-111A")]  // rakam dışı karakter
    public void Validate_Should_Fail_When_CardNumberFormatInvalid(string cardNumber)
    {
        var command = PaymentTestData.PurchaseCommand(Guid.NewGuid(), cardNumber: cardNumber);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_When_InstallmentCountNotAllowed()
    {
        var command = PaymentTestData.PurchaseCommand(Guid.NewGuid(), installmentCount: 4);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_When_QuoteIdEmpty()
    {
        var command = PaymentTestData.PurchaseCommand(Guid.Empty);

        _validator.Validate(command).IsValid.Should().BeFalse();
    }
}
