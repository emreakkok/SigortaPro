using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Staff.DTOs;

namespace SigortaPro.Application.Features.Staff.Queries.GetStaffById;

// ADR-060: Personel detayı (yalnızca Admin). Hedef Personel değilse 404 (varlık sızdırma yok).
public sealed record GetStaffByIdQuery(Guid Id) : IQuery<StaffDetailDto>;
