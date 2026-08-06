using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Auth.DTOs;

namespace SigortaPro.Application.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IRefreshTokenService refreshTokenService,
        IIdentityService identityService,
        ITokenService tokenService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _refreshTokenService = refreshTokenService;
        _identityService = identityService;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var userId = await _refreshTokenService.GetActiveUserIdAsync(request.RefreshToken, cancellationToken);
        if (userId is null)
        {
            return Result<AuthResponse>.Failure("Geçersiz veya süresi dolmuş oturum. Lütfen tekrar giriş yapın.");
        }

        var user = await _identityService.GetByIdAsync(userId.Value, cancellationToken);
        if (user is null)
        {
            // Token geçerli ama kullanıcı silinmiş; token'ı iptal et.
            await _refreshTokenService.RevokeAsync(request.RefreshToken, cancellationToken: cancellationToken);
            return Result<AuthResponse>.Failure("Geçersiz veya süresi dolmuş oturum. Lütfen tekrar giriş yapın.");
        }

        // Pasifleştirilen hesap yeni token alamaz. Eldeki tüm refresh token'lar iptal edilir →
        // en kötü erişim penceresi, mevcut access token'ın kalan ömrü kadardır (≤ 15 dk). Aktiflik sızdırılmaz.
        if (!user.IsActive)
        {
            await _refreshTokenService.RevokeAllForUserAsync(user.Id, cancellationToken);
            return Result<AuthResponse>.Failure("Geçersiz veya süresi dolmuş oturum. Lütfen tekrar giriş yapın.");
        }

        var tokens = _tokenService.CreateTokenPair(user.Id, user.Email, user.Roles);

        // Rotasyon: eski token revoke edilir, yenisi saklanır.
        await _refreshTokenService.RevokeAsync(request.RefreshToken, tokens.RefreshToken, cancellationToken);
        await _refreshTokenService.StoreAsync(user.Id, tokens.RefreshToken, tokens.RefreshTokenExpiresAt, cancellationToken);

        _logger.LogInformation("Token yenilendi. UserId: {UserId}", user.Id);

        return Result<AuthResponse>.Success(new AuthResponse(
            user.Id,
            user.Email,
            user.Roles,
            tokens.AccessToken,
            tokens.AccessTokenExpiresAt,
            tokens.RefreshToken,
            tokens.RefreshTokenExpiresAt));
    }
}
