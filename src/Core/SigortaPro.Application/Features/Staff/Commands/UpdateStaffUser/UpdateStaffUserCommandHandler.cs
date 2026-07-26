using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Staff.DTOs;

namespace SigortaPro.Application.Features.Staff.Commands.UpdateStaffUser;

public sealed class UpdateStaffUserCommandHandler : ICommandHandler<UpdateStaffUserCommand, StaffDetailDto>
{
    private readonly IIdentityService _identityService;

    public UpdateStaffUserCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<StaffDetailDto> Handle(UpdateStaffUserCommand request, CancellationToken cancellationToken)
    {
        // Hedef Personel değilse (bulunamadı/Admin/Customer) false → 404 (varlık sızdırma yok).
        var updated = await _identityService.UpdateStaffFullNameAsync(request.Id, request.FullName, cancellationToken);
        if (!updated)
        {
            throw new NotFoundException("Personel", request.Id);
        }

        var staff = await _identityService.GetStaffByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Personel", request.Id);

        return staff.ToDetailDto();
    }
}
