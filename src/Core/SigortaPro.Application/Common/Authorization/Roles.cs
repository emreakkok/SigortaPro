using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Common.Authorization;

// Rol adlarının tek kaynağı. Domain'deki UserRole enum'undan türetilir (nameof ile derleme zamanı sabiti);
// hem Identity rollerinin seed edilmesinde hem de [Authorize(Roles =...)] attribute'lerinde kullanılır.
public static class Roles
{
    public const string Admin = nameof(UserRole.Admin);
    public const string Personel = nameof(UserRole.Personel);
    public const string Customer = nameof(UserRole.Customer);

    // [Authorize(Roles = ...)] için birleşik sabit (attribute argümanı derleme zamanı sabiti olmak zorunda).
    // Acente personeli (Admin + Personel) tüm müşteri kayıtlarına erişebilir.
    public const string Staff = Admin + "," + Personel;

    // "acente çalışanı kümesi"nin (Admin ∪ Personel) TEK kod içi kaynağı. Attribute'ler derleme
    // zamanı sabiti olan `Staff` string'ini kullanır; çalışma zamanı rol kontrolleri (IsInRole tekrarları,
    // rol bazlı bildirim fan-out'u) bu diziyi kullanır. Böylece staff kümesi tek yerde tanımlıdır.
    public static readonly IReadOnlyList<string> StaffRoles = new[] { Admin, Personel };

    public static readonly IReadOnlyList<string> All = new[] { Admin, Personel, Customer };
}
