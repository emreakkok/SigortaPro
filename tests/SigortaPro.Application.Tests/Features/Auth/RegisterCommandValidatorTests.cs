using FluentAssertions;
using SigortaPro.Application.Features.Auth.Commands.Register;

namespace SigortaPro.Application.Tests.Features.Auth;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    private static RegisterCommand ValidCommand() => new(
        Email: "kullanici@ornek.com",
        Password: "Gecerli!2345",
        FirstName: "Ahmet",
        LastName: "Yilmaz",
        Tckn: "10000000146",
        BirthDate: new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        PhoneNumber: "+905321234567",
        City: "Ankara",
        District: "Cankaya",
        Neighborhood: "Kizilay",
        PostalCode: "06420");

    [Fact]
    public void Validate_Should_Pass_When_CommandIsValid()
    {
        var result = _validator.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_Fail_When_EmailIsInvalid()
    {
        var command = ValidCommand() with { Email = "gecersiz-email" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RegisterCommand.Email));
    }

    [Fact]
    public void Validate_Should_Fail_When_PasswordIsWeak()
    {
        var command = ValidCommand() with { Password = "zayif" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RegisterCommand.Password));
    }

    [Fact]
    public void Validate_Should_Fail_When_TcknIsInvalid()
    {
        var command = ValidCommand() with { Tckn = "12345678901" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RegisterCommand.Tckn));
    }

    [Fact]
    public void Validate_Should_Fail_When_CustomerIsUnder18()
    {
        var command = ValidCommand() with { BirthDate = DateTime.UtcNow.AddYears(-10) };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RegisterCommand.BirthDate));
    }

    [Fact]
    public void Validate_Should_Fail_When_PhoneNumberFormatIsInvalid()
    {
        var command = ValidCommand() with { PhoneNumber = "05321234567" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RegisterCommand.PhoneNumber));
    }
}
