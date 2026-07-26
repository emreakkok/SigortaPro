using Microsoft.AspNetCore.Identity;

namespace SigortaPro.Persistence.Identity;

// ADR-014: Kimlik kullanıcısı Domain'in dışında, Persistence katmanında tanımlanır.
// IdentityUser<Guid> hazır olarak Email, UserName, PasswordHash, lockout vb. alanları sağlar.
// Domain'deki Customer bu kullanıcıya yalnızca AppUserId (Guid) ile bağlanır; navigation yoktur.
public sealed class AppUser : IdentityUser<Guid>
{
    // ADR-061: Hesap yaşam döngüsü. Pasif hesap giriş yapamaz ve token yenileyemez (login/refresh reddi).
    // Migration additive'dir; mevcut satırlar DEFAULT 1 (aktif) alır → geriye dönük erişim korunur.
    public bool IsActive { get; set; } = true;

    // ADR-060: Personel/Admin hesaplarının görünen adı. Customer profili olmayan kimlik hesapları için
    // (bugün staff adı hiçbir yerde tutulmuyordu). Nullable — mevcut hesaplar için doldurulmaz.
    public string? FullName { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();
}
