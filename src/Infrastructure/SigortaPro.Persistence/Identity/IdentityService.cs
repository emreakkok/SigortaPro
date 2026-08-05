using Microsoft.AspNetCore.Identity;
using SigortaPro.Application.Common.Authorization;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;

namespace SigortaPro.Persistence.Identity;

// ADR-014: IIdentityService implementasyonu Persistence katmanında UserManager<AppUser> üzerinden sağlanır.
public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<AppUser> _userManager;

    public IdentityService(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user is not null;
    }

    public async Task<Guid> CreateUserAsync(string email, string password, string role, CancellationToken cancellationToken = default)
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
        };

        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            throw new BusinessRuleException(string.Join(" ", createResult.Errors.Select(error => error.Description)));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            throw new BusinessRuleException(string.Join(" ", roleResult.Errors.Select(error => error.Description)));
        }

        return user.Id;
    }

    public async Task<IdentityUserInfo?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, password))
        {
            return null;
        }

        // ADR-061: Pasif hesap giriş yapamaz. Aktiflik durumu sızdırılmaz — şifre doğru olsa bile null döner,
        // çağıran (LoginCommandHandler) "e-posta veya şifre hatalı" genel mesajını üretir.
        if (!user.IsActive)
        {
            return null;
        }

        return await BuildUserInfoAsync(user);
    }

    public async Task<IdentityUserInfo?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : await BuildUserInfoAsync(user);
    }

    public async Task<string?> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return null;
        }

        // DataProtectorTokenProvider tabanlı token (AddDefaultTokenProviders ile kayıtlı); ömrü
        // DataProtectionTokenProviderOptions ile 1 saate yapılandırılmıştır (ADR-035). Token asla loglanmaz.
        return await _userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return false;
        }

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        return result.Succeeded;
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return false;
        }

        // UserManager mevcut şifreyi doğrular; yanlışsa Succeeded=false döner (ayrıntı sızdırılmaz).
        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        return result.Succeeded;
    }

    public async Task<IReadOnlyList<Guid>> GetUserIdsInRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        var users = await _userManager.GetUsersInRoleAsync(role);
        return users.Select(user => user.Id).ToList();
    }

    // ── ADR-060: Personel (staff) yaşam döngüsü ────────────────────────────────────────────────

    public async Task<IReadOnlyList<StaffUserInfo>> GetStaffUsersAsync(CancellationToken cancellationToken = default)
    {
        var personel = await _userManager.GetUsersInRoleAsync(Roles.Personel);
        return personel
            .Select(user => new StaffUserInfo(user.Id, user.Email!, user.FullName, user.IsActive, new[] { Roles.Personel }))
            .ToList();
    }

    public async Task<StaffUserInfo?> GetStaffByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null || !await _userManager.IsInRoleAsync(user, Roles.Personel))
        {
            // Bulunamadı ya da hedef Personel değil (Admin/Customer) → varlık sızdırma yok (IDOR savunması).
            return null;
        }

        return new StaffUserInfo(user.Id, user.Email!, user.FullName, user.IsActive, new[] { Roles.Personel });
    }

    public async Task<Guid> CreateStaffUserAsync(string email, string fullName, string password, CancellationToken cancellationToken = default)
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FullName = fullName,
            IsActive = true,
            // Admin tarafından oluşturulan hesap güvenilirdir; e-posta doğrulama akışı gerektirmez.
            EmailConfirmed = true,
        };

        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            throw new BusinessRuleException(string.Join(" ", createResult.Errors.Select(error => error.Description)));
        }

        // ADR-060 / güvenlik: rol SUNUCUDA sabittir — her koşulda yalnızca Personel atanır.
        var roleResult = await _userManager.AddToRoleAsync(user, Roles.Personel);
        if (!roleResult.Succeeded)
        {
            throw new BusinessRuleException(string.Join(" ", roleResult.Errors.Select(error => error.Description)));
        }

        return user.Id;
    }

    public async Task<bool> UpdateStaffFullNameAsync(Guid id, string fullName, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null || !await _userManager.IsInRoleAsync(user, Roles.Personel))
        {
            return false;
        }

        user.FullName = fullName;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> SetStaffActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        // Hedef Personel değilse (Admin dahil) işlem yapılmaz → hiçbir Admin pasifleştirilemez (son-Admin invariant'ı).
        if (user is null || !await _userManager.IsInRoleAsync(user, Roles.Personel))
        {
            return false;
        }

        user.IsActive = isActive;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    private async Task<IdentityUserInfo> BuildUserInfoAsync(AppUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new IdentityUserInfo(user.Id, user.Email!, roles.ToList())
        {
            IsActive = user.IsActive,
            FullName = user.FullName,
        };
    }
}
