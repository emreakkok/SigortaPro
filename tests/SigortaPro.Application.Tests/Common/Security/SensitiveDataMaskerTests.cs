using FluentAssertions;
using SigortaPro.Application.Common.Security;

namespace SigortaPro.Application.Tests.Common.Security;

public class SensitiveDataMaskerTests
{
    [Fact]
    public void MaskTckn_Should_RevealOnlyLastTwoDigits_When_TcknIsValidLength()
    {
        SensitiveDataMasker.MaskTckn("11111111110").Should().Be("*********10");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12")]
    public void MaskTckn_Should_ReturnFullyMasked_When_TcknIsNullOrTooShort(string? tckn)
    {
        SensitiveDataMasker.MaskTckn(tckn).Should().Be("***********");
    }

    [Fact]
    public void MaskTckn_Should_NotContainOriginalValue()
    {
        var masked = SensitiveDataMasker.MaskTckn("11111111110");

        masked.Should().NotBe("11111111110");
        masked.Should().HaveLength(11);
    }

    [Fact]
    public void MaskCardNumber_Should_RevealOnlyLastFourDigits_When_CardIsValid()
    {
        SensitiveDataMasker.MaskCardNumber("4111111111111111").Should().Be("************1111");
    }

    [Fact]
    public void MaskCardNumber_Should_StripSeparators_When_CardHasSpaces()
    {
        SensitiveDataMasker.MaskCardNumber("4111 1111 1111 1111").Should().Be("************1111");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123")]
    public void MaskCardNumber_Should_ReturnFullyMasked_When_CardIsNullOrTooShort(string? cardNumber)
    {
        SensitiveDataMasker.MaskCardNumber(cardNumber).Should().Be("****");
    }
}
