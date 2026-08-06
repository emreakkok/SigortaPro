namespace SigortaPro.Application.Common.Interfaces;

// Implementasyonu WebAPI katmanında (HttpContext tabanlı) sağlanır
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsInRole(string role);
}
