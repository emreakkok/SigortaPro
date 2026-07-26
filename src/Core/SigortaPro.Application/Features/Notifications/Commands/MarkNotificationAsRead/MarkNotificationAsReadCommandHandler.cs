using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Notifications.Commands.MarkNotificationAsRead;

public sealed class MarkNotificationAsReadCommandHandler : ICommandHandler<MarkNotificationAsReadCommand>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public MarkNotificationAsReadCommandHandler(
        INotificationRepository notificationRepository,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var notification = await _notificationRepository.GetTrackedByIdAsync(request.NotificationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Notification), request.NotificationId);

        // Kaynak sahipliği: bildirim yalnızca alıcısı tarafından okundu işaretlenebilir (DEVELOPMENT_RULES §7).
        if (notification.RecipientUserId != userId)
        {
            throw new ForbiddenAccessException("Bu bildirim size ait değil.");
        }

        notification.MarkAsRead(_dateTimeProvider.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
