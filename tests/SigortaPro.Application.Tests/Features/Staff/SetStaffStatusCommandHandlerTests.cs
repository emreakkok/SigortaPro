using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Staff.Commands.SetStaffStatus;

namespace SigortaPro.Application.Tests.Features.Staff;

// ADR-060/061: Aktif/pasif handler'ı. Pasifleştirmede token iptali; hedef Personel değilse 404 (son-Admin invariant'ı).
public class SetStaffStatusCommandHandlerTests
{
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly IRefreshTokenService _refreshTokenService = Substitute.For<IRefreshTokenService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly SetStaffStatusCommandHandler _handler;

    public SetStaffStatusCommandHandlerTests()
    {
        _handler = new SetStaffStatusCommandHandler(
            _identityService, _refreshTokenService, _currentUserService,
            Substitute.For<ILogger<SetStaffStatusCommandHandler>>());
    }

    [Fact]
    public async Task Handle_Should_RevokeAllTokens_When_Deactivating()
    {
        var staffId = Guid.NewGuid();
        _identityService.SetStaffActiveAsync(staffId, false, Arg.Any<CancellationToken>()).Returns(true);

        await _handler.Handle(new SetStaffStatusCommand(staffId, false), CancellationToken.None);

        await _refreshTokenService.Received(1).RevokeAllForUserAsync(staffId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_NotRevokeTokens_When_Activating()
    {
        var staffId = Guid.NewGuid();
        _identityService.SetStaffActiveAsync(staffId, true, Arg.Any<CancellationToken>()).Returns(true);

        await _handler.Handle(new SetStaffStatusCommand(staffId, true), CancellationToken.None);

        await _refreshTokenService.DidNotReceive().RevokeAllForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact] // Hedef Personel değil (Admin/Customer/bulunamayan) → 404; hiçbir Admin pasifleştirilemez.
    public async Task Handle_Should_Throw_When_TargetIsNotPersonel()
    {
        var targetId = Guid.NewGuid();
        _identityService.SetStaffActiveAsync(targetId, false, Arg.Any<CancellationToken>()).Returns(false);

        var act = () => _handler.Handle(new SetStaffStatusCommand(targetId, false), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _refreshTokenService.DidNotReceive().RevokeAllForUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
