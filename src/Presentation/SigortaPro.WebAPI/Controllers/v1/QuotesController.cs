using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SigortaPro.Application.Common.Authorization;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Quotes.Commands.ApproveQuote;
using SigortaPro.Application.Features.Quotes.Commands.CreateQuote;
using SigortaPro.Application.Features.Quotes.Commands.RejectQuote;
using SigortaPro.Application.Features.Quotes.DTOs;
using SigortaPro.Application.Features.Quotes.Queries.GetQuoteById;
using SigortaPro.Application.Features.Quotes.Queries.GetQuoteComparison;
using SigortaPro.Application.Features.Quotes.Queries.GetQuoteList;

namespace SigortaPro.WebAPI.Controllers.v1;

[ApiController]
[Route("api/v1/quotes")]
[Authorize]
public sealed class QuotesController : ControllerBase
{
    private readonly ISender _sender;

    public QuotesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Branş + risk objesi + teminat paketi seçerek teklif oluşturur (fiyatlama motoru çağrılır).</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType(typeof(QuoteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateQuote(CreateQuoteCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetQuoteById), new { id = result.Id }, result);
    }

    /// <summary>Aynı risk objesi için teminat seviyeli alternatif paketleri üretir (önizleme, teklif oluşturmaz).</summary>
    [HttpGet("compare")]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType(typeof(QuoteComparisonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Compare([FromQuery] GetQuoteComparisonQuery query, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Teklif listesini getirir: müşteri kendi tekliflerini, acente personeli tümünü görür.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<QuoteSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQuotes([FromQuery] GetQuoteListQuery query, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Teklif detayını (prim dökümü ile) getirir. Sahibi müşteri veya acente personeli erişebilir.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(QuoteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuoteById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetQuoteByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>Fiyatlandırılmış teklifi onaylar (Priced → Approved).</summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType(typeof(QuoteSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ApproveQuote(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ApproveQuoteCommand(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>Teklifi reddeder (satın alınmış/süresi dolmuş teklif hariç → Rejected).</summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType(typeof(QuoteSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RejectQuote(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RejectQuoteCommand(id), cancellationToken);
        return Ok(result);
    }
}
