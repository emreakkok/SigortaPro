using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SigortaPro.Application.Common.Authorization;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Staff.Commands.CreateStaffUser;
using SigortaPro.Application.Features.Staff.Commands.SetStaffStatus;
using SigortaPro.Application.Features.Staff.Commands.UpdateStaffUser;
using SigortaPro.Application.Features.Staff.DTOs;
using SigortaPro.Application.Features.Staff.Queries.GetStaffById;
using SigortaPro.Application.Features.Staff.Queries.GetStaffList;

namespace SigortaPro.WebAPI.Controllers.v1;

// ADR-060: Personel (staff) yönetimi. TÜM yüzey yalnızca Admin'e açıktır (Personel ve Customer 403 alır).
// Rol atama, şifre sıfırlama ve silme uçları bilinçli olarak YOKTUR (§26.4). Route deseni mevcut
// controller konvansiyonuyla aynıdır (api/v1/{kaynak}); yetki attribute ile ifade edilir, yol adıyla değil.
[ApiController]
[Route("api/v1/staff")]
[Authorize(Roles = Roles.Admin)]
public sealed class StaffController : ControllerBase
{
    private readonly ISender _sender;

    public StaffController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Personel listesini arama (e-posta/ad) ve aktiflik filtresiyle sayfalı getirir (yalnızca Admin).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<StaffListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStaff([FromQuery] GetStaffListQuery query, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Belirtilen personelin detayını getirir. Hedef personel değilse 404 (yalnızca Admin).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(StaffDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStaffById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetStaffByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>Yeni bir Personel hesabı oluşturur. Rol sunucuda sabittir; istemci rol gönderemez (yalnızca Admin).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(StaffDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateStaff(CreateStaffUserCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetStaffById), new { id = result.Id }, result);
    }

    /// <summary>Personelin görünen adını günceller (e-posta/rol değişmez; yalnızca Admin).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(StaffDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStaff(Guid id, UpdateStaffRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpdateStaffUserCommand(id, request.FullName), cancellationToken);
        return Ok(result);
    }

    /// <summary>Personeli aktif/pasif yapar. Pasifleştirmede oturumları iptal edilir. Hedef personel değilse 404 (yalnızca Admin).</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetStatus(Guid id, SetStaffStatusRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new SetStaffStatusCommand(id, request.IsActive), cancellationToken);
        return NoContent();
    }
}

// Güncelleme istek gövdesi; personel kimliği route'tan alınır. Rol/e-posta/aktiflik alanları BİLİNÇLİ olarak yoktur.
public sealed record UpdateStaffRequest(string FullName);

// Aktiflik istek gövdesi; personel kimliği route'tan alınır.
public sealed record SetStaffStatusRequest(bool IsActive);
