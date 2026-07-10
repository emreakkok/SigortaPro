using FluentAssertions;
using SigortaPro.Application.Common.Validation;

namespace SigortaPro.Application.Tests.Common.Validation;

public class TcknValidationTests
{
    [Theory]
    [InlineData("10000000146")]
    [InlineData("12345678950")]
    public void IsValid_Should_ReturnTrue_When_TcknIsAlgorithmicallyValid(string tckn)
    {
        TcknValidation.IsValid(tckn).Should().BeTrue();
    }

    [Theory]
    [InlineData("12345678901")] // kontrol basamakları hatalı
    [InlineData("00000000000")] // ilk hane 0
    [InlineData("1234567890")]  // 10 hane
    [InlineData("123456789012")] // 12 hane
    [InlineData("1234567890A")] // rakam olmayan karakter
    [InlineData("")]
    [InlineData(null)]
    public void IsValid_Should_ReturnFalse_When_TcknIsInvalid(string? tckn)
    {
        TcknValidation.IsValid(tckn).Should().BeFalse();
    }
}
