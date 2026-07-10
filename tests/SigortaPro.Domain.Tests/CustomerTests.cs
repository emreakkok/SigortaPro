using FluentAssertions;
using SigortaPro.Domain.Common;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Domain.Tests;

public class CustomerTests
{
    [Theory]
    [InlineData("")]
    [InlineData("123456789")]
    [InlineData("123456789012")]
    public void Constructor_Should_ThrowDomainException_When_TcknIsNotElevenDigits(string invalidTckn)
    {
        var act = () => new Customer(Guid.NewGuid(), "Ahmet", "Yılmaz", invalidTckn, new DateTime(1990, 1, 1), "+905551112233", CreateAddress());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_Should_CreateCustomer_When_TcknIsElevenDigits()
    {
        var customer = new Customer(Guid.NewGuid(), "Ahmet", "Yılmaz", "12345678901", new DateTime(1990, 1, 1), "+905551112233", CreateAddress());

        customer.Tckn.Should().Be("12345678901");
    }

    [Fact]
    public void UpdateContactInfo_Should_UpdatePhoneAndAddress_When_Called()
    {
        var customer = new Customer(Guid.NewGuid(), "Ahmet", "Yılmaz", "12345678901", new DateTime(1990, 1, 1), "+905551112233", CreateAddress());
        var newAddress = new Address("Ankara", "Çankaya", "Kızılay", "06420");

        customer.UpdateContactInfo("+905559998877", newAddress);

        customer.PhoneNumber.Should().Be("+905559998877");
        customer.Address.Should().Be(newAddress);
    }

    private static Address CreateAddress()
    {
        return new Address("İstanbul", "Kadıköy", "Caferağa", "34710");
    }
}
