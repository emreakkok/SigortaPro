using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Notifications.DTOs;

namespace SigortaPro.Application.Features.Notifications.Queries.GetMyNotifications;

// Oturum sahibinin bildirim geçmişi: en yeni önce; okunma/önem/metin/tarih filtreleri + sayfalama.
public sealed record GetMyNotificationsQuery(
    bool? IsRead = null,
    string? Severity = null,
    string? SearchTerm = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<NotificationDto>>;
