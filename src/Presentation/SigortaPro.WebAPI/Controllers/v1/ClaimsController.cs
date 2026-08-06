using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SigortaPro.Application.Common.Authorization;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Claims.Commands.ApproveClaim;
using SigortaPro.Application.Features.Claims.Commands.CreateClaim;
using SigortaPro.Application.Features.Claims.Commands.PayClaim;
using SigortaPro.Application.Features.Claims.Commands.RejectClaim;
using SigortaPro.Application.Features.Claims.Commands.StartClaimReview;
using SigortaPro.Application.Features.Claims.DTOs;
using SigortaPro.Application.Features.Claims.Queries.GetClaimById;
using SigortaPro.Application.Features.Claims.Queries.GetClaimDocument;
using SigortaPro.Application.Features.Claims.Queries.GetClaimList;

namespace SigortaPro.WebAPI.Controllers.v1;

[ApiController]
[Route("api/v1/claims")]
[Authorize]
public sealed class ClaimsController : ControllerBase
{
    private readonly ISender _sender;

    public ClaimsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Aktif bir poliçe için hasar bildirir (olay bilgisi + mock foto yükleme metadatası).</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateClaim(CreateClaimCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetClaimById), new { id = result.Id }, result);
    }

    /// <summary>Hasar listesini getirir: müşteri kendi hasarlarını, acente personeli tümünü görür.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ClaimSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClaims([FromQuery] GetClaimListQuery query, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Hasar detayını getirir. Sahibi müşteri veya acente personeli erişebilir.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClaimById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetClaimByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>Hasara eklenen bir belgenin (foto/PDF) içeriğini döndürür. Sahibi müşteri veya acente personeli erişebilir.</summary>
    [HttpGet("{id:guid}/documents/{documentId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClaimDocument(Guid id, Guid documentId, CancellationToken cancellationToken)
    {
        var document = await _sender.Send(new GetClaimDocumentQuery(id, documentId), cancellationToken);
        // Tarayıcıda satır içi görüntülenebilsin (görsel önizleme / PDF açma); indirme adı da korunur.
        return File(document.Content, document.ContentType, document.FileName, enableRangeProcessing: false);
    }

    /// <summary>Bildirilen hasarı incelemeye alır (Submitted → UnderReview).</summary>
    [HttpPost("{id:guid}/start-review")]
    [Authorize(Roles = Roles.Staff)]
    [ProducesResponseType(typeof(ClaimSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> StartReview(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new StartClaimReviewCommand(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>İncelemedeki hasarı onaylar (UnderReview → Approved); onay tutarı ve değerlendirme notu kaydedilir.</summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = Roles.Staff)]
    [ProducesResponseType(typeof(ClaimSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Approve(Guid id, ApproveClaimCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command with { ClaimId = id }, cancellationToken);
        return Ok(result);
    }

    /// <summary>İncelemedeki hasarı reddeder (UnderReview → Rejected); gerekçe zorunludur.</summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = Roles.Staff)]
    [ProducesResponseType(typeof(ClaimSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reject(Guid id, RejectClaimCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command with { ClaimId = id }, cancellationToken);
        return Ok(result);
    }

    /// <summary>Onaylanmış hasarın ödemesini gerçekleştirir (Approved → Paid). Yalnızca Admin.</summary>
    [HttpPost("{id:guid}/pay")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(ClaimSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Pay(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new PayClaimCommand(id), cancellationToken);
        return Ok(result);
    }
}
