using FluentAssertions;
using SigortaPro.Application.Features.Auth.Commands.ChangePassword;

namespace SigortaPro.Application.Tests.Features.Auth;

public class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_When_CommandIsValid()
    {
        var result = _validator.Validate(new ChangePasswordCommand("Eski!2345", "Yeni!2345"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_Fail_When_NewPasswordIsWeak()
    {
        var result = _validator.Validate(new ChangePasswordCommand("Eski!2345", "zayif"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(ChangePasswordCommand.NewPassword));
    }

    [Fact]
    public void Validate_Should_Fail_When_NewPasswordEqualsCurrentPassword()
    {
        var result = _validator.Validate(new ChangePasswordCommand("Ayni!2345", "Ayni!2345"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(ChangePasswordCommand.NewPassword));
    }

    [Fact]
    public void Validate_Should_Fail_When_CurrentPasswordIsEmpty()
    {
        var result = _validator.Validate(new ChangePasswordCommand("", "Yeni!2345"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(ChangePasswordCommand.CurrentPassword));
    }
}
