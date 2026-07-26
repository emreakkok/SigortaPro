using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;

namespace SigortaPro.Application.Features.Notifications.Queries.GetUnreadNotificationCount;

public sealed class GetUnreadNotificationCountQueryHandler
    : IQueryHandler<GetUnreadNotificationCountQuery, int>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetUnreadNotificationCountQueryHandler(
        INotificationRepository notificationRepository,
        ICurrentUserService currentUserService)
    {
        _notificationRepository = notificationRepository;
        _currentUserService = currentUserService;
    }

    public Task<int> Handle(GetUnreadNotificationCountQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        return _notificationRepository.CountUnreadAsync(userId, cancellationToken);
    }
}
