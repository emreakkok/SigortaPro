namespace SigortaPro.Application.Common.Models;

// ITokenService tarafından üretilen access + refresh token çifti ve son kullanma zamanları.
// Süreler (access 15 dk, refresh 7 gün) token servisinin (Infrastructure)
// JwtSettings konfigürasyonundan gelir; handler bu değerleri yalnızca taşır.
public sealed record TokenPair(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);
