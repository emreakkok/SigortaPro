namespace SigortaPro.Application.Common.Models;

// IIdentityService'in dış dünyaya (Application handler'larına) döndürdüğü kullanıcı özeti.
// Identity tipini (Persistence'taki AppUser) sızdırmadan yalnızca primitif değerleri taşır (ADR-014).
public sealed record IdentityUserInfo(Guid Id, string Email, IReadOnlyList<string> Roles)
{
    // ADR-061: Hesap aktiflik durumu. Refresh akışı bunu okuyup pasif hesabın token yenilemesini engeller.
    // Varsayılan true → mevcut yapım çağrıları ve testler davranışını değiştirmez (aktif kullanıcı).
    public bool IsActive { get; init; } = true;

    // Personel/Admin hesaplarının görünen adı (ADR-060). Acente destekli teklifte "üreten personel"in adını
    // (herhangi bir staff — Admin dahil) çözmek için kullanılır. Customer hesaplarında/eski kayıtlarda null.
    public string? FullName { get; init; }
}
