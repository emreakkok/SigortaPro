using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SigortaPro.Application.Common.Authorization;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Auth.Commands.Login;

namespace SigortaPro.Application.Tests.Features.Auth;

public class LoginCommandHandlerTests
{
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IRefreshTokenService _refreshTokenService = Substitute.For<IRefreshTokenService>();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(
            _identityService,
            _tokenService,
            _refreshTokenService,
            Substitute.For<ILogger<LoginCommandHandler>>());
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_When_CredentialsAreInvalid()
    {
        _identityService.ValidateCredentialsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IdentityUserInfo?)null);

        var result = await _handler.Handle(new LoginCommand("kullanici@ornek.com", "yanlis"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _tokenService.DidNotReceive().CreateTokenPair(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>());
    }

    [Fact]
    public async Task Handle_Should_ReturnTokens_When_CredentialsAreValid()
    {
        var userId = Guid.NewGuid();
        _identityService.ValidateCredentialsAsync("kullanici@ornek.com", "dogru", Arg.Any<CancellationToken>())
            .Returns(new IdentityUserInfo(userId, "kullanici@ornek.com", new[] { Roles.Customer }));
        _tokenService.CreateTokenPair(userId, "kullanici@ornek.com", Arg.Any<IEnumerable<string>>())
            .Returns(new TokenPair("access-token", DateTime.UtcNow.AddMinutes(15), "refresh-token", DateTime.UtcNow.AddDays(7)));

        var result = await _handler.Handle(new LoginCommand("kullanici@ornek.com", "dogru"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("access-token");
        await _refreshTokenService.Received(1).StoreAsync(
            userId, "refresh-token", Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }
}
