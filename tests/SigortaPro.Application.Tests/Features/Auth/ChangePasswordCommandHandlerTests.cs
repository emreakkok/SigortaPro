using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Auth.Commands.ChangePassword;

namespace SigortaPro.Application.Tests.Features.Auth;

public class ChangePasswordCommandHandlerTests
{
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly ChangePasswordCommandHandler _handler;

    public ChangePasswordCommandHandlerTests()
    {
        _handler = new ChangePasswordCommandHandler(
            _identityService,
            _currentUserService,
            Substitute.For<ILogger<ChangePasswordCommandHandler>>());
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_When_CurrentPasswordIsCorrect()
    {
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns(userId);
        _identityService.ChangePasswordAsync(userId, "Eski!2345", "Yeni!2345", Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _handler.Handle(new ChangePasswordCommand("Eski!2345", "Yeni!2345"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_When_CurrentPasswordIsWrong()
    {
        _currentUserService.UserId.Returns(Guid.NewGuid());
        _identityService.ChangePasswordAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _handler.Handle(new ChangePasswordCommand("Yanlis!2345", "Yeni!2345"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_Should_ThrowForbidden_When_UserIdIsMissing()
    {
        _currentUserService.UserId.Returns((Guid?)null);

        var act = () => _handler.Handle(new ChangePasswordCommand("Eski!2345", "Yeni!2345"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
