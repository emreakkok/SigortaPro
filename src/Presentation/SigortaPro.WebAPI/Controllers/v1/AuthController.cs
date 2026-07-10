using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SigortaPro.Application.Features.Auth.Commands.Login;
using SigortaPro.Application.Features.Auth.Commands.RefreshToken;
using SigortaPro.Application.Features.Auth.Commands.Register;
using SigortaPro.Application.Features.Auth.DTOs;
using SigortaPro.WebAPI.Extensions;

namespace SigortaPro.WebAPI.Controllers.v1;

[ApiController]
[Route("api/v1/auth")]
// Kimlik doğrulama uçları brute-force denemelerine karşı rate limit ile korunur (DEVELOPMENT_RULES.md §7).
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
}
