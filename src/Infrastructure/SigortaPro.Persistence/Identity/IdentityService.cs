using Microsoft.AspNetCore.Identity;
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

        return await BuildUserInfoAsync(user);
    }

    public async Task<IdentityUserInfo?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : await BuildUserInfoAsync(user);
    }

    private async Task<IdentityUserInfo> BuildUserInfoAsync(AppUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new IdentityUserInfo(user.Id, user.Email!, roles.ToList());
    }
}
