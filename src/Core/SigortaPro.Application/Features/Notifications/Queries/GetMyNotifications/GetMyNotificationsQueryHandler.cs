using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Notifications.DTOs;

namespace SigortaPro.Application.Features.Notifications.Queries.GetMyNotifications;

public sealed class GetMyNotificationsQueryHandler
    : IQueryHandler<GetMyNotificationsQuery, PagedResult<NotificationDto>>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetMyNotificationsQueryHandler(
        INotificationRepository notificationRepository,
        ICurrentUserService currentUserService)
    {
        _notificationRepository = notificationRepository;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<NotificationDto>> Handle(
        GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var page = await _notificationRepository.GetPagedForRecipientAsync(
            userId, request.IsRead, request.Severity, request.SearchTerm, request.From, request.To,
            new PaginationParams { Page = request.Page, PageSize = request.PageSize },
            cancellationToken);

        var items = page.Items
            .Select(notification => new NotificationDto(
                notification.Id,
                notification.Type,
                notification.Severity,
                notification.Title,
                notification.Message,
                notification.RelatedEntityId,
                notification.RelatedEntityType,
                notification.IsRead,
                notification.ReadAt,
                notification.CreatedAt,
                notification.ActorName,
                notification.ReferenceCode))
            .ToList();

        return new PagedResult<NotificationDto>(items, page.Page, page.PageSize, page.TotalCount);
    }
}
