using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Common.Authorization;

// Rol adlarının tek kaynağı. Domain'deki UserRole enum'undan türetilir (nameof ile derleme zamanı sabiti);
// hem Identity rollerinin seed edilmesinde hem de [Authorize(Roles = ...)] attribute'lerinde kullanılır (ADR-014).
public static class Roles
{
    public const string Admin = nameof(UserRole.Admin);
    public const string Personel = nameof(UserRole.Personel);
    public const string Customer = nameof(UserRole.Customer);

    // [Authorize(Roles = ...)] için birleşik sabit (attribute argümanı derleme zamanı sabiti olmak zorunda).
    // Acente personeli (Admin + Personel) tüm müşteri kayıtlarına erişebilir (PROJECT_CONTEXT.md §3).
    public const string Staff = Admin + "," + Personel;

    public static readonly IReadOnlyList<string> All = new[] { Admin, Personel, Customer };
}
