using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Staff.DTOs;

namespace SigortaPro.Application.Features.Staff.Queries.GetStaffById;

public sealed class GetStaffByIdQueryHandler : IQueryHandler<GetStaffByIdQuery, StaffDetailDto>
{
    private readonly IIdentityService _identityService;

    public GetStaffByIdQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<StaffDetailDto> Handle(GetStaffByIdQuery request, CancellationToken cancellationToken)
    {
        // GetStaffByIdAsync yalnızca Personel için değer döner; Admin/Customer/bulunamayan → null → 404.
        var staff = await _identityService.GetStaffByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Personel", request.Id);

        return staff.ToDetailDto();
    }
}
