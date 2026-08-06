using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;

namespace SigortaPro.Application.Features.Staff.Commands.SetStaffStatus;

public sealed class SetStaffStatusCommandHandler : ICommandHandler<SetStaffStatusCommand>
{
    private readonly IIdentityService _identityService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SetStaffStatusCommandHandler> _logger;

    public SetStaffStatusCommandHandler(
        IIdentityService identityService,
        IRefreshTokenService refreshTokenService,
        ICurrentUserService currentUserService,
        ILogger<SetStaffStatusCommandHandler> logger)
    {
        _identityService = identityService;
        _refreshTokenService = refreshTokenService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task Handle(SetStaffStatusCommand request, CancellationToken cancellationToken)
    {
        // Hedef Personel değilse (bulunamadı/Admin/Customer) false → 404. Böylece hiçbir Admin
        // (son Admin dahil) pasifleştirilemez — son-Admin invariant'ı yapısal olarak korunur.
        var changed = await _identityService.SetStaffActiveAsync(request.Id, request.IsActive, cancellationToken);
        if (!changed)
        {
            throw new NotFoundException("Personel", request.Id);
        }

        // Pasifleştirmede eldeki tüm refresh token'lar iptal edilir → yenileme anında kesilir.
        if (!request.IsActive)
        {
            await _refreshTokenService.RevokeAllForUserAsync(request.Id, cancellationToken);
        }

        _logger.LogInformation(
            "Personel aktiflik durumu değiştirildi. AdminUserId: {AdminUserId}, StaffUserId: {StaffUserId}, IsActive: {IsActive}",
            _currentUserService.UserId, request.Id, request.IsActive);
    }
}
