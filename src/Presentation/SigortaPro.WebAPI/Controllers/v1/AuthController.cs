using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SigortaPro.Application.Features.Auth.Commands.ChangePassword;
using SigortaPro.Application.Features.Auth.Commands.ForgotPassword;
using SigortaPro.Application.Features.Auth.Commands.Login;
using SigortaPro.Application.Features.Auth.Commands.RefreshToken;
using SigortaPro.Application.Features.Auth.Commands.Register;
using SigortaPro.Application.Features.Auth.Commands.ResetPassword;
using SigortaPro.Application.Features.Auth.DTOs;
using SigortaPro.WebAPI.Extensions;

namespace SigortaPro.WebAPI.Controllers.v1;

[ApiController]
[Route("api/v1/auth")]
// Kimlik doğrulama uçları brute-force denemelerine karşı rate limit ile korunur.
[EnableRateLimiting(WebApiConstants.AuthRateLimitPolicy)]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Müşteri kaydı: Identity kullanıcısı + Customer profili oluşturur ve oturum açar.</summary>
    /// <returns>Access ve refresh token içeren oturum bilgisi.</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : Conflict(new { errors = result.Errors });
    }

    /// <summary>E-posta ve şifre ile giriş yapar.</summary>
    /// <returns>Access ve refresh token içeren oturum bilgisi.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : Unauthorized(new { errors = result.Errors });
    }

    /// <summary>Geçerli bir refresh token ile yeni access + refresh token üretir (rotasyonlu).</summary>
    /// <returns>Yeni access ve refresh token içeren oturum bilgisi.</returns>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : Unauthorized(new { errors = result.Errors });
    }

    /// <summary>Şifre sıfırlama talebi: kayıtlı e-postaya sıfırlama bağlantısı gönderir.</summary>
    /// <remarks>Güvenlik gereği (kullanıcı varlığını sızdırmama) e-posta kayıtlı olsun ya da olmasın
    /// her zaman aynı generic başarı yanıtı döner.</remarks>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        // Sonuç bilinçli olarak yok sayılır: akış her koşulda başarı döner (enumeration koruması).
        await _sender.Send(command, cancellationToken);
        return Ok(new { message = "Eğer bu e-posta adresi kayıtlıysa, şifre sıfırlama bağlantısı gönderildi." });
    }

    /// <summary>Oturum sahibinin şifresini mevcut şifre doğrulamasıyla değiştirir.</summary>
    /// <returns>Başarılıysa 200; mevcut şifre hatalıysa 400.</returns>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? Ok(new { message = "Şifreniz başarıyla değiştirildi." })
            : BadRequest(new { errors = result.Errors });
    }

    /// <summary>Geçerli bir sıfırlama token'ı ile yeni şifreyi belirler.</summary>
    /// <returns>Başarılıysa 200; token geçersiz/süresi dolmuşsa 400.</returns>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? Ok(new { message = "Şifreniz başarıyla güncellendi. Yeni şifrenizle giriş yapabilirsiniz." })
            : BadRequest(new { errors = result.Errors });
    }
}
