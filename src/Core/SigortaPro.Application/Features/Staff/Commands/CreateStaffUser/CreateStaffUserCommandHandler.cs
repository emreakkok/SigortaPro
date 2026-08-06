using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Staff.DTOs;

namespace SigortaPro.Application.Features.Staff.Commands.CreateStaffUser;

public sealed class CreateStaffUserCommandHandler : ICommandHandler<CreateStaffUserCommand, StaffDetailDto>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateStaffUserCommandHandler> _logger;

    public CreateStaffUserCommandHandler(
        IIdentityService identityService,
        ICurrentUserService currentUserService,
        ILogger<CreateStaffUserCommandHandler> logger)
    {
        _identityService = identityService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<StaffDetailDto> Handle(CreateStaffUserCommand request, CancellationToken cancellationToken)
    {
        // E-posta çakışmasını önden kontrol et (DbUpdateException/500 yerine anlamlı 409 üretmek için — Register deseni).
        if (await _identityService.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            throw new BusinessRuleException("Bu e-posta adresi ile daha önce bir hesap oluşturulmuş.");
        }

        // Rol SUNUCUDA sabittir: CreateStaffUserAsync her koşulda Roles.Personel atar.
        var staffId = await _identityService.CreateStaffUserAsync(
            request.Email, request.FullName, request.Password, cancellationToken);

        // KVKK/güvenlik: şifre ASLA loglanmaz; yalnızca kim-kimi-oluşturdu kimliği yapılandırılmış log'a yazılır.
        _logger.LogInformation(
            "Personel hesabı oluşturuldu. AdminUserId: {AdminUserId}, StaffUserId: {StaffUserId}",
            _currentUserService.UserId, staffId);

        var created = await _identityService.GetStaffByIdAsync(staffId, cancellationToken)
            ?? throw new NotFoundException("Personel", staffId);

        return created.ToDetailDto();
    }
}
