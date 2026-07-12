using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SigortaPro.Application.Common.Authorization;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Payments.Commands.PurchaseQuote;
using SigortaPro.Application.Features.Payments.DTOs;
using SigortaPro.Application.Features.Payments.Queries.GetInstallmentOptions;
using SigortaPro.Application.Features.Payments.Queries.GetPaymentHistory;

namespace SigortaPro.WebAPI.Controllers.v1;

[ApiController]
[Route("api/v1/payments")]
[Authorize]
public sealed class PaymentsController : ControllerBase
{
    private readonly ISender _sender;

    public PaymentsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Onaylanmış teklif için mock sanal POS ile ödeme alır; başarılıysa aktif poliçe oluşturur.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType(typeof(PurchaseResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Purchase(PurchaseQuoteCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Oturum sahibi müşterinin ödeme geçmişini (başarılı/başarısız denemeler) sayfalı getirir.</summary>
    [HttpGet]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType(typeof(PagedResult<PaymentHistoryItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory([FromQuery] GetPaymentHistoryQuery query, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Onaylanmış teklifin primi için taksit seçeneklerini getirir (ödeme sayfası önizlemesi).</summary>
    [HttpGet("installment-options")]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType(typeof(IReadOnlyList<InstallmentOptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetInstallmentOptions([FromQuery] GetInstallmentOptionsQuery query, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }
}
