using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;

namespace SigortaPro.Application.Features.Notifications.Commands.MarkAllNotificationsAsRead;

public sealed class MarkAllNotificationsAsReadCommandHandler : ICommandHandler<MarkAllNotificationsAsReadCommand>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public MarkAllNotificationsAsReadCommandHandler(
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

    public async Task Handle(MarkAllNotificationsAsReadCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var unread = await _notificationRepository.GetUnreadTrackedAsync(userId, cancellationToken);
        if (unread.Count == 0)
        {
            return;
        }

        var now = _dateTimeProvider.UtcNow;
        foreach (var notification in unread)
        {
            notification.MarkAsRead(now);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
