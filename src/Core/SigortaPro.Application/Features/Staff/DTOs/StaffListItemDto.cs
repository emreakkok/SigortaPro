namespace SigortaPro.Application.Features.Staff.DTOs;

// Personel liste satırı. Hassas Identity alanları (şifre hash'i, token, lockout) taşınmaz.
public sealed record StaffListItemDto(Guid Id, string Email, string? FullName, bool IsActive);
