namespace SigortaPro.Application.Features.Staff.DTOs;

// Personel detayı. Roller her zaman ["Personel"]'dir (bu yüzey yalnızca Personel'i döndürür).
public sealed record StaffDetailDto(Guid Id, string Email, string? FullName, bool IsActive, IReadOnlyList<string> Roles);
