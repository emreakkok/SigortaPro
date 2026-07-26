using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Auth.Commands.ResetPassword;

namespace SigortaPro.Application.Tests.Features.Auth;

public class ResetPasswordCommandHandlerTests
{
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly ResetPasswordCommandHandler _handler;

    public ResetPasswordCommandHandlerTests()
    {
        _handler = new ResetPasswordCommandHandler(
            _identityService,
            Substitute.For<ILogger<ResetPasswordCommandHandler>>());
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_When_ResetSucceeds()
    {
        _identityService.ResetPasswordAsync("kullanici@ornek.com", "token", "Gecerli!2345", Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _handler.Handle(
            new ResetPasswordCommand("kullanici@ornek.com", "token", "Gecerli!2345"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_When_TokenIsInvalidOrExpired()
    {
        _identityService.ResetPasswordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _handler.Handle(
            new ResetPasswordCommand("kullanici@ornek.com", "gecersiz-token", "Gecerli!2345"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().NotBeEmpty();
    }
}
