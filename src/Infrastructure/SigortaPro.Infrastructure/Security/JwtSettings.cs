namespace SigortaPro.Infrastructure.Security;

// JWT üretimi ve doğrulaması için ortak ayarlar. appsettings.json "JwtSettings" bölümünden bağlanır.
// TokenService (Infrastructure) ve JWT bearer doğrulaması (WebAPI) aynı ayarları paylaşır.
public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;

    // DEVELOPMENT_RULES.md §7: access token 15 dk, refresh token 7 gün.
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 7;
}
