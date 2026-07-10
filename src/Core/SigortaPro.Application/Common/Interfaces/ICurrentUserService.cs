namespace SigortaPro.Application.Common.Interfaces;

// Implementasyonu WebAPI katmanında (HttpContext tabanlı) sağlanır — bkz. ARCHITECTURE_RULES.md §6.1.
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsInRole(string role);
}
