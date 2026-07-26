using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Staff.DTOs;

namespace SigortaPro.Application.Features.Staff.Queries.GetStaffList;

// ADR-060: Personel listesi (yalnızca Admin). Arama e-posta/ad üzerinde; aktiflik filtresi opsiyonel.
public sealed record GetStaffListQuery(
    int Page = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    bool? IsActive = null) : IQuery<PagedResult<StaffListItemDto>>;
