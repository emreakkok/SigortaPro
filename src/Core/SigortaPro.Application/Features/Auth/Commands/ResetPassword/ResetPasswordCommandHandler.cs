using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;

namespace SigortaPro.Application.Features.Auth.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand, Result>
{
    private readonly IIdentityService _identityService;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IIdentityService identityService,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _identityService = identityService;
        _logger = logger;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var succeeded = await _identityService.ResetPasswordAsync(
            request.Email, request.Token, request.NewPassword, cancellationToken);

        if (!succeeded)
        {
            // Hesap yok / token geçersiz veya süresi dolmuş: hangisinin hatalı olduğu sızdırılmaz (güvenlik).
            _logger.LogWarning("Geçersiz veya süresi dolmuş şifre sıfırlama denemesi.");
            return Result.Failure("Şifre sıfırlama bağlantısı geçersiz veya süresi dolmuş. Lütfen yeni bir talep oluşturun.");
        }

        _logger.LogInformation("Kullanıcı şifresini başarıyla sıfırladı.");
        return Result.Success();
    }
}
