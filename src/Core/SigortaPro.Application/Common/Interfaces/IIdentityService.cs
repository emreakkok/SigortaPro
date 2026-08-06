using SigortaPro.Application.Common.Models;

namespace SigortaPro.Application.Common.Interfaces;

// Kullanıcı/kimlik işlemlerinin Application soyutlaması. Implementasyonu Persistence katmanında
// UserManager<AppUser> kullanılarak sağlanır.
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

    // Şifre sıfırlama token'ı üretir (ASP.NET Core Identity DataProtectorTokenProvider).
    // Kullanıcı bulunamazsa null döner (varlık sızdırmamak için çağıran katman bunu sessizce ele alır).
    Task<string?> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken = default);

    // Verilen token ile şifreyi sıfırlar. Kullanıcı yoksa veya token geçersiz/süresi dolmuşsa false döner
    // (hangisinin hatalı olduğu bilgisi sızdırılmaz — güvenlik).
    Task<bool> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default);

    // Oturum sahibinin şifresini mevcut şifre doğrulamasıyla değiştirir. Kullanıcı yoksa veya
    // mevcut şifre yanlışsa false döner (hangisinin hatalı olduğu sızdırılmaz).
    Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

    // Verilen roldeki kullanıcıların Id'lerini döner (— kalıcı bildirim fan-out'u için;
    // staff kitlesi Admin ∪ Personel olarak çağıran tarafça birleştirilir).
    Task<IReadOnlyList<Guid>> GetUserIdsInRoleAsync(string role, CancellationToken cancellationToken = default);

    // ── Personel (staff) yaşam döngüsü ────────────────────────────────────────────────
    // Yalnızca `Personel` rolündeki kullanıcıları döner (Admin'ler listeye dahil edilmez). Filtreleme
    // ve sayfalama çağıran handler'da yapılır (MVP ölçeğinde personel sayısı düşük).
    Task<IReadOnlyList<StaffUserInfo>> GetStaffUsersAsync(CancellationToken cancellationToken = default);

    // Verilen Id `Personel` rolünde değilse (bulunamadı, Admin veya Customer) null döner — varlık sızdırmaz
    // ve Admin/Customer kimliklerinin bu yüzeyden okunmasını (IDOR) engeller.
    Task<StaffUserInfo?> GetStaffByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Personel hesabı oluşturur. Rol SUNUCUDA `Roles.Personel` olarak sabitlenir (istemciden alınmaz).
    // E-posta çakışması/şifre politikası ihlalinde BusinessRuleException fırlatır. Oluşan kullanıcının Id'sini döner.
    Task<Guid> CreateStaffUserAsync(string email, string fullName, string password, CancellationToken cancellationToken = default);

    // Personelin görünen adını günceller. Hedef `Personel` değilse false döner (404 semantiği). Rol/e-posta değişmez.
    Task<bool> UpdateStaffFullNameAsync(Guid id, string fullName, CancellationToken cancellationToken = default);

    // Personelin aktiflik durumunu değiştirir. Hedef `Personel` DEĞİLSE (Admin dahil) false döner — böylece
    // hiçbir Admin (dolayısıyla son Admin de) pasifleştirilemez (son-Admin invariant'ı yapısal olarak korunur).
    // Token iptali bu metodun sorumluluğu değildir; çağıran handler IRefreshTokenService ile yürütür.
    Task<bool> SetStaffActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
}
