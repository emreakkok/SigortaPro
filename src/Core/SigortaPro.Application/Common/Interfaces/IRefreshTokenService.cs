namespace SigortaPro.Application.Common.Interfaces;

// Refresh token'ların kalıcı yönetimi (saklama, doğrulama, rotasyon). Refresh token kaydı Identity'nin
// yanında Persistence katmanında tutulur (ADR-014); implementasyonu Persistence'tadır.
public interface IRefreshTokenService
{
    Task StoreAsync(Guid userId, string token, DateTime expiresAt, CancellationToken cancellationToken = default);

    // Token aktif (revoke edilmemiş ve süresi dolmamış) ise ilgili kullanıcının Id'sini, aksi halde null döner.
    Task<Guid?> GetActiveUserIdAsync(string token, CancellationToken cancellationToken = default);

    // Rotasyon: verilen token'ı revoke eder ve (verildiyse) hangi token ile değiştirildiğini işaretler.
    Task RevokeAsync(string token, string? replacedByToken = null, CancellationToken cancellationToken = default);

    // ADR-061: Bir kullanıcının tüm aktif refresh token'larını iptal eder. Personel pasifleştirildiğinde
    // çağrılır → eldeki oturumlar anında yenilenemez hale gelir (en kötü erişim penceresi = access token ömrü ≤ 15 dk).
    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
