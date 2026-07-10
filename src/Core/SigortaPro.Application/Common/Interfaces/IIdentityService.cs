using SigortaPro.Application.Common.Models;

namespace SigortaPro.Application.Common.Interfaces;

// Kullanıcı/kimlik işlemlerinin Application soyutlaması. Implementasyonu Persistence katmanında
// UserManager<AppUser> kullanılarak sağlanır (ADR-014, ARCHITECTURE_RULES.md §6.1).
// Identity tipleri (AppUser vb.) bu arayüzün dışına sızmaz; yalnızca primitif değerler/DTO'lar taşınır.
public interface IIdentityService
{
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    // Kullanıcıyı oluşturur ve verilen role atar; oluşturulan kullanıcının Id'sini döner.
    // Identity oluşturma başarısız olursa (örn. şifre politikası) BusinessRuleException fırlatır.
    Task<Guid> CreateUserAsync(string email, string password, string role, CancellationToken cancellationToken = default);

    // E-posta + şifre doğrulaması. Kullanıcı bulunamazsa veya şifre hatalıysa null döner
    // (hangisinin hatalı olduğu bilgisini sızdırmaz — güvenlik).
    Task<IdentityUserInfo?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<IdentityUserInfo?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
