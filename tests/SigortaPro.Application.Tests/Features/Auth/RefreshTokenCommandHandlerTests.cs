using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SigortaPro.Application.Common.Authorization;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Auth.Commands.RefreshToken;

namespace SigortaPro.Application.Tests.Features.Auth;

public class RefreshTokenCommandHandlerTests
{
    private readonly IRefreshTokenService _refreshTokenService = Substitute.For<IRefreshTokenService>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _handler = new RefreshTokenCommandHandler(
            _refreshTokenService,
            _identityService,
            _tokenService,
            Substitute.For<ILogger<RefreshTokenCommandHandler>>());
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_When_TokenIsNotActive()
    {
        _refreshTokenService.GetActiveUserIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var result = await _handler.Handle(new RefreshTokenCommand("gecersiz"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _tokenService.DidNotReceive().CreateTokenPair(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>());
    }

    [Fact]
    public async Task Handle_Should_RevokeAndReturnFailure_When_UserNoLongerExists()
    {
        var userId = Guid.NewGuid();
        _refreshTokenService.GetActiveUserIdAsync("aktif", Arg.Any<CancellationToken>()).Returns(userId);
        _identityService.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((IdentityUserInfo?)null);

        var result = await _handler.Handle(new RefreshTokenCommand("aktif"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _refreshTokenService.Received(1).RevokeAsync("aktif", Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_RotateTokenAndReturnNewTokens_When_TokenIsActive()
    {
        var userId = Guid.NewGuid();
        _refreshTokenService.GetActiveUserIdAsync("eski-token", Arg.Any<CancellationToken>()).Returns(userId);
        _identityService.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new IdentityUserInfo(userId, "kullanici@ornek.com", new[] { Roles.Customer }));
        _tokenService.CreateTokenPair(userId, "kullanici@ornek.com", Arg.Any<IEnumerable<string>>())
            .Returns(new TokenPair("yeni-access", DateTime.UtcNow.AddMinutes(15), "yeni-refresh", DateTime.UtcNow.AddDays(7)));

        var result = await _handler.Handle(new RefreshTokenCommand("eski-token"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("yeni-access");
        result.Value.RefreshToken.Should().Be("yeni-refresh");

        // Eski token yenisiyle değiştirilerek revoke edilmeli, yeni token saklanmalı.
        await _refreshTokenService.Received(1).RevokeAsync("eski-token", "yeni-refresh", Arg.Any<CancellationToken>());
        await _refreshTokenService.Received(1).StoreAsync(
            userId, "yeni-refresh", Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }
}
