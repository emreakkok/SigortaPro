using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Staff.DTOs;

namespace SigortaPro.Application.Features.Staff;

// StaffUserInfo (kimlik özeti) → feature DTO eşlemeleri. Tek yerde toplanır.
internal static class StaffMappings
{
    public static StaffListItemDto ToListItemDto(this StaffUserInfo info) =>
        new(info.Id, info.Email, info.FullName, info.IsActive);

    public static StaffDetailDto ToDetailDto(this StaffUserInfo info) =>
        new(info.Id, info.Email, info.FullName, info.IsActive, info.Roles);
}
