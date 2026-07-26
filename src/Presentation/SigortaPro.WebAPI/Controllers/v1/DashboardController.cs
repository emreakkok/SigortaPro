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

    /// <summary>
    /// Operasyon dashboard'ının tüm blokları tek çağrıda (ADR-052): seçilen aralığın KPI'ları + önceki eşit
    /// uzunluktaki dönemle karşılaştırma, aksiyon merkezi, prim üretimi zaman serisi, satış hunisi,
    /// branş performansı, hasar operasyonu ve portföy. Aralık verilmezse son 30 gün kullanılır.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSummary(
        [FromQuery] GetDashboardSummaryQuery query, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
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

    /// <summary>Tarih aralıklı ödeme/ciro raporu (işlem tarihine göre; sayfalı). Yalnızca Admin (ADR-060: ciro görünürlüğü yönetimseldir; sınıf düzeyi Staff yetkisini Admin'e daraltır).</summary>
    [HttpGet("reports/payments")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(PagedResult<PaymentReportItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPaymentReport(
        [FromQuery] GetPaymentReportQuery query, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>En riskli müşteri segmentleri (hasar sayısına göre ilk N). Yalnızca Admin (P1 kararı D3: hasar tutarı + müşteri profilleme birleşimi yönetimsel/KVKK hassasiyetlidir).</summary>
    [HttpGet("reports/riskiest-customers")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerRiskSegmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRiskiestCustomers(
        [FromQuery] GetRiskiestCustomersQuery query, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }
}
