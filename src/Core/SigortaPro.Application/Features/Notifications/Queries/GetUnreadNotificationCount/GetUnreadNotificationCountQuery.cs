using SigortaPro.Application.Common.Interfaces;

namespace SigortaPro.Application.Features.Notifications.Queries.GetUnreadNotificationCount;

// Zil rozeti için oturum sahibinin okunmamış bildirim sayısı (ADR-042).
public sealed record GetUnreadNotificationCountQuery : IQuery<int>;
