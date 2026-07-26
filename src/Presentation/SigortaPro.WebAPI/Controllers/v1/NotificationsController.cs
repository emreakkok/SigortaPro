using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Notifications.Commands.MarkAllNotificationsAsRead;
using SigortaPro.Application.Features.Notifications.Commands.MarkNotificationAsRead;
using SigortaPro.Application.Features.Notifications.DTOs;
using SigortaPro.Application.Features.Notifications.Queries.GetMyNotifications;
using SigortaPro.Application.Features.Notifications.Queries.GetUnreadNotificationCount;

namespace SigortaPro.WebAPI.Controllers.v1;

// ADR-042: Kalıcı bildirim merkezi uçları. Tüm yüzey oturum sahibinin KENDİ bildirimleriyle sınırlıdır
// (alıcı bazlı satır modeli); staff bildirimleri şu an aktif kitledir, müşteri alıcılığı hazırdır.
[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly ISender _sender;

    public NotificationsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Oturum sahibinin bildirim geçmişi (okunma/önem/metin/tarih filtreleri + sayfalama).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyNotifications([FromQuery] GetMyNotificationsQuery query, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Okunmamış bildirim sayısı (zil rozeti).</summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var count = await _sender.Send(new GetUnreadNotificationCountQuery(), cancellationToken);
        return Ok(count);
    }

    /// <summary>Tek bildirimi okundu işaretler (yalnızca alıcısı — aksi 403).</summary>
    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new MarkNotificationAsReadCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Oturum sahibinin tüm okunmamış bildirimlerini okundu işaretler.</summary>
    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        await _sender.Send(new MarkAllNotificationsAsReadCommand(), cancellationToken);
        return NoContent();
    }
}
