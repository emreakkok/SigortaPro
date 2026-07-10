using Microsoft.AspNetCore.Identity;

namespace SigortaPro.Persistence.Identity;

// ADR-014: Kimlik kullanıcısı Domain'in dışında, Persistence katmanında tanımlanır.
// IdentityUser<Guid> hazır olarak Email, UserName, PasswordHash, lockout vb. alanları sağlar.
// Domain'deki Customer bu kullanıcıya yalnızca AppUserId (Guid) ile bağlanır; navigation yoktur.
public sealed class AppUser : IdentityUser<Guid>
{
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();
}
