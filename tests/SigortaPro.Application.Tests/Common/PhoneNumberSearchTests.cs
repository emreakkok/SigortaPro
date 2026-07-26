using FluentAssertions;
using SigortaPro.Application.Common.Search;

namespace SigortaPro.Application.Tests.Common;

// Telefonla format-bağımsız arama normalizasyonu (PhoneNumberSearch). Saklama kanoniktir ("+90XXXXXXXXXX");
// girdi serbesttir. Farklı yazımlar AYNI abone son ekine indirgenmeli → aynı müşteriyi bulmalı.
public class PhoneNumberSearchTests
{
    [Theory]
    [InlineData("05551111111", "5551111111")]        // ulusal, baştaki 0
    [InlineData("0555 111 11 11", "5551111111")]      // boşluklu
    [InlineData("0555-111-11-11", "5551111111")]      // tireli
    [InlineData("+90 555 111 11 11", "5551111111")]   // uluslararası, boşluklu
    [InlineData("+905551111111", "5551111111")]        // kanonik (saklama biçimi)
    [InlineData("5551111111", "5551111111")]           // yalın abone numarası
    [InlineData("905551111111", "5551111111")]         // ülke kodu, +'sız
    public void ToSubscriberDigits_Should_ReduceAllFormatsToSameSubscriber(string input, string expected)
    {
        PhoneNumberSearch.ToSubscriberDigits(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Ahmet")]
    public void ToSubscriberDigits_Should_ReturnEmpty_When_NoDigits(string? input)
    {
        PhoneNumberSearch.ToSubscriberDigits(input).Should().BeEmpty();
    }

    [Fact]
    public void ToSubscriberDigits_Should_StripLeadingZeroFromPartialInput()
    {
        // Kısmi girdi de son ek olarak kullanılır (Contains ile eşleşir).
        PhoneNumberSearch.ToSubscriberDigits("0555").Should().Be("555");
    }

    [Theory]
    [InlineData("555", true)]
    [InlineData("5551111111", true)]
    [InlineData("55", false)]   // çok kısa → telefon eşleşmesi uygulanmaz
    [InlineData("Ahmet", false)]
    [InlineData("", false)]
    public void HasUsablePhoneQuery_Should_RequireMinimumDigits(string? term, bool expected)
    {
        PhoneNumberSearch.HasUsablePhoneQuery(term).Should().Be(expected);
    }
}
