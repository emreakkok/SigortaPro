using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;

namespace SigortaPro.Application.Features.Auth.Commands.ForgotPassword;

// Güvenlik ilkeleri:
//  - Kullanıcı varlığı hiçbir şekilde sızdırılmaz: e-posta kayıtlı olsun ya da olmasın sonuç aynıdır (Success).
//  - SMTP gönderimi başarısız olsa bile kullanıcıya hata dönmez; yalnızca loglanır (tiplenmiş EmailDeliveryException).
//  - Reset token'ı ve link hiçbir log kaydına yazılmaz.
public sealed class ForgotPasswordCommandHandler : ICommandHandler<ForgotPasswordCommand, Result>
{
    private readonly IIdentityService _identityService;
    private readonly IPasswordResetNotifier _passwordResetNotifier;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        IIdentityService identityService,
        IPasswordResetNotifier passwordResetNotifier,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _identityService = identityService;
        _passwordResetNotifier = passwordResetNotifier;
        _logger = logger;
    }

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var resetToken = await _identityService.GeneratePasswordResetTokenAsync(request.Email, cancellationToken);

        if (resetToken is null)
        {
            // Kayıtlı olmayan e-posta: e-posta gönderilmez ama yine de generic başarı döner (enumeration koruması).
            // Log, hangi e-postanın sorgulandığını göstermez.
            _logger.LogInformation("Kayıtlı olmayan bir hesap için şifre sıfırlama talebi alındı; e-posta gönderilmedi.");
            return Result.Success();
        }

        try
        {
            await _passwordResetNotifier.SendResetLinkAsync(request.Email, resetToken, cancellationToken);
            _logger.LogInformation("Şifre sıfırlama e-postası gönderildi.");
        }
        catch (EmailDeliveryException exception)
        {
            // Gönderim hatası kullanıcıya sızdırılmaz; operasyonel amaçla loglanır (hassas veri içermez).
            _logger.LogError(exception, "Şifre sıfırlama e-postası gönderilemedi.");
        }

        return Result.Success();
    }
}
