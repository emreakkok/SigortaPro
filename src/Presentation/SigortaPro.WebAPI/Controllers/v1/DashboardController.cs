using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SigortaPro.Application.Common.Authorization;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Dashboard.DTOs;
using SigortaPro.Application.Features.Dashboard.Queries.GetDashboardSummary;
using SigortaPro.Application.Features.Dashboard.Queries.GetPaymentReport;
using SigortaPro.Application.Features.Dashboard.Queries.GetPolicyReport;
using SigortaPro.Application.Features.Dashboard.Queries.GetRiskiestCustomers;

namespace SigortaPro.WebAPI.Controllers.v1;

// Admin panelinin veri kaynağı. Yalnızca acente personeli (Admin+Personel) erişebilir; tüm uçlar salt okunur.
[ApiController]
[Route("api/v1/dashboard")]
[Authorize(Roles = Roles.Staff)]
public sealed class DashboardController : ControllerBase
{
    private readonly ISender _sender;

    public DashboardController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Dashboard özet metriklerini getirir (prim üretimi, aktif poliçe, bekleyenler, oranlar, aylık trend, branş dağılımı).</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDashboardSummaryQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Tarih aralıklı poliçe raporu (başlangıç tarihine göre; sayfalı).</summary>
    [HttpGet("reports/policies")]
    [ProducesResponseType(typeof(PagedResult<PolicyReportItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPolicyReport(
        [FromQuery] GetPolicyReportQuery query, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Tarih aralıklı ödeme raporu (işlem tarihine göre; sayfalı).</summary>
    [HttpGet("reports/payments")]
    [ProducesResponseType(typeof(PagedResult<PaymentReportItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPaymentReport(
        [FromQuery] GetPaymentReportQuery query, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>En riskli müşteri segmentleri (hasar sayısına göre ilk N).</summary>
    [HttpGet("reports/riskiest-customers")]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerRiskSegmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRiskiestCustomers(
        [FromQuery] GetRiskiestCustomersQuery query, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }
}
