using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SigortaPro.Application.Common.Authorization;
using SigortaPro.Application.Features.Pricing.Commands.CreatePricingVersion;
using SigortaPro.Application.Features.Pricing.DTOs;
using SigortaPro.Application.Features.Pricing.Queries.GetPricingVersions;

namespace SigortaPro.WebAPI.Controllers.v1;

// ADR-048: Fiyatlandırma (tarife) yönetimi. **Yalnızca Admin** — personel ve müşteri erişemez:
// tarife, acentenin ticari parametresidir ve operasyonel bir işlem değildir.
// Versiyonlar değişmezdir: güncelleme/silme ucu bilinçli olarak YOKTUR (fiyat değişikliği = yeni versiyon).
[ApiController]
[Route("api/v1/pricing")]
[Authorize(Roles = Roles.Admin)]
public sealed class PricingController : ControllerBase
{
    private readonly ISender _sender;

    public PricingController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Yürürlükteki tarife + tüm fiyatlandırma geçmişi (en yeni önce).</summary>
    [HttpGet("versions")]
    [ProducesResponseType(typeof(IReadOnlyList<PricingVersionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PricingVersionDto>>> GetVersions(CancellationToken cancellationToken)
    {
        var versions = await _sender.Send(new GetPricingVersionsQuery(), cancellationToken);
        return Ok(versions);
    }

    /// <summary>
    /// Yeni tarife versiyonu yayınlar. Yalnızca bu andan SONRA oluşturulacak teklifleri etkiler;
    /// mevcut teklif ve poliçelerin primleri değişmez (sabitlenmiş versiyonlarıyla hesaplanmaya devam eder).
    /// </summary>
    [HttpPost("versions")]
    [ProducesResponseType(typeof(PricingVersionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PricingVersionDto>> CreateVersion(
        CreatePricingVersionCommand command, CancellationToken cancellationToken)
    {
        var version = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetVersions), new { }, version);
    }
}
