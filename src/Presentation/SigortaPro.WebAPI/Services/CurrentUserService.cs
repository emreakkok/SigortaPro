using System.Security.Claims;
using SigortaPro.Application.Common.Interfaces;

namespace SigortaPro.WebAPI.Services;

// ARCHITECTURE_RULES.md §6.1: ICurrentUserService implementasyonu WebAPI'de HttpContext tabanlı sağlanır.
// JWT'den gelen claim'leri okuyarak aktif kullanıcı bilgisini Application katmanına taşır (kaynak sahipliği kontrolü için).
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var userId) ? userId : null;
        }
    }

    public string? Email => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);

    public IReadOnlyList<string> Roles =>
        _httpContextAccessor.HttpContext?.User
            .FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .ToList()
        ?? new List<string>();

    public bool IsInRole(string role) =>
        _httpContextAccessor.HttpContext?.User.IsInRole(role) ?? false;
}
