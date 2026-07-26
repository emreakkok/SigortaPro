using FluentAssertions;
using SigortaPro.Application.Features.Auth.Commands.ForgotPassword;

namespace SigortaPro.Application.Tests.Features.Auth;

public class ForgotPasswordCommandValidatorTests
{
    private readonly ForgotPasswordCommandValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_When_EmailIsValid()
    {
        var result = _validator.Validate(new ForgotPasswordCommand("kullanici@ornek.com"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("gecersiz-email")]
    public void Validate_Should_Fail_When_EmailIsMissingOrInvalid(string email)
    {
        var result = _validator.Validate(new ForgotPasswordCommand(email));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(ForgotPasswordCommand.Email));
    }
}
