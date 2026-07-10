namespace SigortaPro.Application.Features.Auth.DTOs;

// Kayıt / giriş / token yenileme sonrası döndürülen oturum bilgisi.
// Hassas veri (şifre hash'i, ham TCKN) içermez (CODING_STANDARDS.md §4.2).
public sealed record AuthResponse(
    Guid UserId,
    string Email,
    IReadOnlyList<string> Roles,
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);
