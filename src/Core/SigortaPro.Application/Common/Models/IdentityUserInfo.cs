namespace SigortaPro.Application.Common.Models;

// IIdentityService'in dış dünyaya (Application handler'larına) döndürdüğü kullanıcı özeti.
// Identity tipini (Persistence'taki AppUser) sızdırmadan yalnızca primitif değerleri taşır.
public sealed record IdentityUserInfo(Guid Id, string Email, IReadOnlyList<string> Roles)
{
    // Hesap aktiflik durumu. Refresh akışı bunu okuyup pasif hesabın token yenilemesini engeller.
    // Varsayılan true → mevcut yapım çağrıları ve testler davranışını değiştirmez (aktif kullanıcı).
    public bool IsActive { get; init; } = true;

    // Personel/Admin hesaplarının görünen adı. Acente destekli teklifte "üreten personel"in adını
    // (herhangi bir staff — Admin dahil) çözmek için kullanılır. Customer hesaplarında/eski kayıtlarda null.
    public string? FullName { get; init; }
}
