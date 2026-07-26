using SigortaPro.Application.Common.Interfaces;

namespace SigortaPro.Application.Features.Staff.Commands.SetStaffStatus;

// ADR-060/061: Personel aktif/pasif yapma (yalnızca Admin). Hedef yalnızca Personel olabilir → hiçbir Admin
// pasifleştirilemez (son-Admin invariant'ı). Pasifleştirmede kullanıcının tüm refresh token'ları iptal edilir.
// Id route'tan gelir. Yanıt gövdesi yoktur (204).
public sealed record SetStaffStatusCommand(Guid Id, bool IsActive) : ICommand;
