using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Staff.DTOs;

namespace SigortaPro.Application.Features.Staff;

// ADR-060: StaffUserInfo (kimlik özeti) → feature DTO eşlemeleri. Tek yerde toplanır (CODING_STANDARDS §5.2).
internal static class StaffMappings
{
    public static StaffListItemDto ToListItemDto(this StaffUserInfo info) =>
        new(info.Id, info.Email, info.FullName, info.IsActive);

    public static StaffDetailDto ToDetailDto(this StaffUserInfo info) =>
        new(info.Id, info.Email, info.FullName, info.IsActive, info.Roles);
}
