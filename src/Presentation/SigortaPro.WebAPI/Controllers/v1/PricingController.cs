using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SigortaPro.Application.Common.Authorization;
using SigortaPro.Application.Features.Pricing.Commands.ActivatePricingVersion;
using SigortaPro.Application.Features.Pricing.Commands.CreatePricingVersion;
using SigortaPro.Application.Features.Pricing.Commands.DiscardPricingDraft;
using SigortaPro.Application.Features.Pricing.Commands.UpdatePricingDraft;
using SigortaPro.Application.Features.Pricing.DTOs;
using SigortaPro.Application.Features.Pricing.Queries.GetPricingVersions;

namespace SigortaPro.WebAPI.Controllers.v1;

// ADR-048: Fiyatlandırma (tarife) yönetimi. Okuma acente personeline (Admin/Personel) açıktır; ancak DEĞİŞİKLİK
// (taslak oluştur/düzenle/aktifleştir) **YALNIZCA Admin**'e açıktır — personel yalnızca görüntüler, müşteri hiç erişemez.
// Aktif/arşiv versiyonlar değişmezdir: yalnızca TASLAK düzenlenir; fiyat değişikliği = yeni versiyon + aktifleştirme.
[ApiController]
[Route("api/v1/pricing")]
[Authorize(Roles = Roles.Staff)]
public sealed class PricingController : ControllerBase
{
    private readonly ISender _sender;

    public PricingController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Yürürlükteki (aktif) tarife + taslak + tüm fiyatlandırma geçmişi (Admin/Personel görüntüler).</summary>
    [HttpGet("versions")]
    [ProducesResponseType(typeof(IReadOnlyList<PricingVersionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PricingVersionDto>>> GetVersions(CancellationToken cancellationToken)
    {
        var versions = await _sender.Send(new GetPricingVersionsQuery(), cancellationToken);
        return Ok(versions);
    }

    /// <summary>
    /// Yeni bir TASLAK tarife versiyonu oluşturur (aktif tarifeden seed edilir). Canlı fiyatları ETKİLEMEZ;
    /// açık taslak varsa mevcut taslak döner. Yalnızca Admin.
    /// </summary>
    [HttpPost("versions")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(PricingVersionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PricingVersionDto>> CreateDraft(
        CreatePricingVersionCommand command, CancellationToken cancellationToken)
    {
        var version = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetVersions), new { }, version);
    }

    /// <summary>
    /// TASLAK versiyonu düzenler (baz primler + paket/şehir/yenileme kaldıraçları). Yalnızca taslak
    /// düzenlenebilir; aktif/arşiv versiyon değiştirilemez. Canlı fiyatları etkilemez. Yalnızca Admin.
    /// </summary>
    [HttpPut("versions/{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(PricingVersionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PricingVersionDto>> UpdateDraft(
        Guid id, UpdatePricingDraftCommand command, CancellationToken cancellationToken)
    {
        var version = await _sender.Send(command with { VersionId = id }, cancellationToken);
        return Ok(version);
    }

    /// <summary>
    /// TASLAK versiyonu AKTİFLEŞTİRİR. Bu andan SONRAKİ teklifler yeni tarifeyi kullanır; mevcut teklif/poliçe
    /// primleri değişmez. Önceki aktif versiyon arşivlenir. Yalnızca Admin.
    /// </summary>
    [HttpPost("versions/{id:guid}/activate")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(PricingVersionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PricingVersionDto>> Activate(Guid id, CancellationToken cancellationToken)
    {
        var version = await _sender.Send(new ActivatePricingVersionCommand(id), cancellationToken);
        return Ok(version);
    }

    /// <summary>
    /// Kullanılmayan bir TASLAK versiyonu iptal eder (soft-delete). Aktif/arşiv versiyon iptal edilemez.
    /// Yalnızca Admin.
    /// </summary>
    [HttpDelete("versions/{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DiscardDraft(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DiscardPricingDraftCommand(id), cancellationToken);
        return NoContent();
    }
}
