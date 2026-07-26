using FluentAssertions;
using SigortaPro.Application.Features.Auth.Commands.ResetPassword;

namespace SigortaPro.Application.Tests.Features.Auth;

public class ResetPasswordCommandValidatorTests
{
    private readonly ResetPasswordCommandValidator _validator = new();

    private static ResetPasswordCommand ValidCommand() =>
        new(Email: "kullanici@ornek.com", Token: "gecerli-token", NewPassword: "Gecerli!2345");

    [Fact]
    public void Validate_Should_Pass_When_CommandIsValid()
    {
        var result = _validator.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_Fail_When_TokenIsEmpty()
    {
        var command = ValidCommand() with { Token = string.Empty };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(ResetPasswordCommand.Token));
    }

    [Fact]
    public void Validate_Should_Fail_When_NewPasswordIsWeak()
    {
        var command = ValidCommand() with { NewPassword = "zayif" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(ResetPasswordCommand.NewPassword));
    }

    [Fact]
    public void Validate_Should_Fail_When_EmailIsInvalid()
    {
        var command = ValidCommand() with { Email = "gecersiz" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(ResetPasswordCommand.Email));
    }
}
