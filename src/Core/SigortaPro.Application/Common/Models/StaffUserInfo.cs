namespace SigortaPro.Application.Common.Models;

// Personel (staff) yönetimi için IIdentityService'in döndürdüğü kimlik özeti. Identity tipini
// (AppUser) sızdırmaz; yalnızca yönetim yüzeyinin ihtiyaç duyduğu primitifleri taşır. Şifre hash'i,
// token, güvenlik damgası, lockout gibi hassas alanlar bilinçli olarak DIŞARIDA bırakılmıştır (KVKK — minimizasyon).
public sealed record StaffUserInfo(
    Guid Id,
    string Email,
    string? FullName,
    bool IsActive,
    IReadOnlyList<string> Roles);
