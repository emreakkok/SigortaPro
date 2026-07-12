using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SigortaPro.Application.Common.Authorization;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Renewals.Commands.AcceptRenewal;
using SigortaPro.Application.Features.Renewals.DTOs;
using SigortaPro.Application.Features.Renewals.Queries.GetMyRenewals;

namespace SigortaPro.WebAPI.Controllers.v1;

[ApiController]
[Route("api/v1/renewals")]
[Authorize(Roles = Roles.Customer)]
public sealed class RenewalsController : ControllerBase
{
    private readonly ISender _sender;

    public RenewalsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Oturum sahibi müşterinin yenileme tekliflerini sayfalı getirir.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<RenewalDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyRenewals([FromQuery] GetMyRenewalsQuery query, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Yenileme teklifini onaylar; yeni dönem teklifi Approved'a çekilir (ardından ödeme akışına bağlanır).</summary>
    [HttpPost("{id:guid}/accept")]
    [ProducesResponseType(typeof(RenewalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Accept(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new AcceptRenewalCommand(id), cancellationToken);
        return Ok(result);
    }
}
