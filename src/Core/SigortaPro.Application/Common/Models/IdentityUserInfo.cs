namespace SigortaPro.Application.Common.Models;

// IIdentityService'in dış dünyaya (Application handler'larına) döndürdüğü kullanıcı özeti.
// Identity tipini (Persistence'taki AppUser) sızdırmadan yalnızca primitif değerleri taşır (ADR-014).
public sealed record IdentityUserInfo(Guid Id, string Email, IReadOnlyList<string> Roles);
