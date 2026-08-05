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

    // ── Acente destekli teklif (agent-assisted): personel müşteri ADINA teklif oluşturur ──────────────────
    // Gerçek akış: müşteri acenteyi telefonla arar; personel teklifi hazırlar. Teklifin SAHİBİ yine müşteridir.
    // Personel yalnızca oluşturur — onay/ödeme/poliçeleştirme uçları Customer'a kilitlidir (aşağıdaki
    // approve/reject + PaymentsController) → personel müşteri adına satın alamaz/onaylayamaz (yapısal güvence).

    /// <summary>Acente personeli, seçtiği müşteri adına teklif oluşturur (müşteri sonra kendi onaylar/satın alır).</summary>
    [HttpPost("~/api/v1/customers/{customerId:guid}/quotes")]
    [Authorize(Roles = Roles.Staff)]
    [ProducesResponseType(typeof(QuoteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateQuoteForCustomer(
        Guid customerId, CreateQuoteCommand command, CancellationToken cancellationToken)
    {
        // Hedef müşteri ROUTE'tan alınır; gövdedeki CustomerId (varsa) yok sayılır (spoofing önlemi).
        var result = await _sender.Send(command with { CustomerId = customerId }, cancellationToken);
        return CreatedAtAction(nameof(GetQuoteById), new { id = result.Id }, result);
    }

    /// <summary>Acente personeli için seçili müşteri adına paket karşılaştırması (önizleme; teklif oluşturmaz).</summary>
    [HttpGet("~/api/v1/customers/{customerId:guid}/quotes/compare")]
    [Authorize(Roles = Roles.Staff)]
    [ProducesResponseType(typeof(QuoteComparisonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareForCustomer(
        Guid customerId, [FromQuery] GetQuoteComparisonQuery query, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query with { CustomerId = customerId }, cancellationToken);
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
