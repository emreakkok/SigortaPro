using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Auth.Commands.ForgotPassword;

namespace SigortaPro.Application.Tests.Features.Auth;

public class ForgotPasswordCommandHandlerTests
{
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly IPasswordResetNotifier _passwordResetNotifier = Substitute.For<IPasswordResetNotifier>();
    private readonly ForgotPasswordCommandHandler _handler;

    public ForgotPasswordCommandHandlerTests()
    {
        _handler = new ForgotPasswordCommandHandler(
            _identityService,
            _passwordResetNotifier,
            Substitute.For<ILogger<ForgotPasswordCommandHandler>>());
    }

    [Fact]
    public async Task Handle_Should_SendResetLink_When_UserExists()
    {
        _identityService.GeneratePasswordResetTokenAsync("kullanici@ornek.com", Arg.Any<CancellationToken>())
            .Returns("reset-token");

        var result = await _handler.Handle(new ForgotPasswordCommand("kullanici@ornek.com"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _passwordResetNotifier.Received(1).SendResetLinkAsync(
            "kullanici@ornek.com", "reset-token", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccessWithoutSendingEmail_When_UserDoesNotExist()
    {
        // Enumeration koruması: kayıtlı olmayan e-postada da e-posta gönderilmez ama sonuç yine başarı.
        _identityService.GeneratePasswordResetTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var result = await _handler.Handle(new ForgotPasswordCommand("yok@ornek.com"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _passwordResetNotifier.DidNotReceive().SendResetLinkAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_When_EmailDeliveryFails()
    {
        // SMTP hatası kullanıcıya sızdırılmaz; akış yine generic başarı döner (ADR-035).
        _identityService.GeneratePasswordResetTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("reset-token");
        _passwordResetNotifier.SendResetLinkAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new EmailDeliveryException("gönderilemedi", new InvalidOperationException())));

        var result = await _handler.Handle(new ForgotPasswordCommand("kullanici@ornek.com"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
