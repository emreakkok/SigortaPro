namespace SigortaPro.Persistence.Identity;

// Refresh token kaydı Identity'nin yanında Persistence katmanında tutulur (ADR-014). Domain BaseEntity'sinden
// türemez; bir Domain entity'si değil, kimlik doğrulama altyapısının parçasıdır. Rotasyon için revoke/replace
// alanları taşır (DEVELOPMENT_RULES.md §7).
public sealed class RefreshToken
{
    public Guid Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }

    public bool IsActive(DateTime utcNow) => RevokedAt is null && utcNow < ExpiresAt;
}
