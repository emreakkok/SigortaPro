using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;

namespace SigortaPro.Application.Features.Auth.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand, Result>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(
        IIdentityService identityService,
        ICurrentUserService currentUserService,
        ILogger<ChangePasswordCommandHandler> logger)
    {
        _identityService = identityService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        // Uç [Authorize] olduğundan normalde dolu gelir; guard yine de savunmacı tutulur.
        var userId = _currentUserService.UserId
            ?? throw new ForbiddenAccessException("Bu işlem için oturum açmanız gerekir.");

        var succeeded = await _identityService.ChangePasswordAsync(
            userId, request.CurrentPassword, request.NewPassword, cancellationToken);

        if (!succeeded)
        {
            // Mevcut şifre yanlış (veya kullanıcı bulunamadı) — ayrıntı sızdırılmaz, şifreler loglanmaz.
            _logger.LogWarning("Başarısız şifre değiştirme denemesi. UserId: {UserId}", userId);
            return Result.Failure("Mevcut şifreniz hatalı.");
        }

        _logger.LogInformation("Kullanıcı şifresini değiştirdi. UserId: {UserId}", userId);
        return Result.Success();
    }
}
