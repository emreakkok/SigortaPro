using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SigortaPro.Application.Features.Cities.DTOs;
using SigortaPro.Application.Features.Cities.Queries.GetCityCatalog;

namespace SigortaPro.WebAPI.Controllers.v1;

// Post-MVP (ADR-037/ADR-039): İl kataloğu (Türkiye'nin 81 ili) — adres formlarındaki aranabilir combobox verisi.
// Salt okunur, hassas olmayan kamu referans verisi; kayıt (register) formu anonim olduğundan uç [AllowAnonymous]
// olmalıdır — aksi hâlde kayıt sayfasındaki il seçici 401 alıp serbest metne düşer (ADR-039).
[ApiController]
[Route("api/v1/city-catalog")]
[AllowAnonymous]
public sealed class CityCatalogController : ControllerBase
{
    private readonly ISender _sender;

    public CityCatalogController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>İl (81 il) kataloğunu getirir (adres formu combobox'ı için).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(CityCatalogDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCatalog(CancellationToken cancellationToken)
    {
        var catalog = await _sender.Send(new GetCityCatalogQuery(), cancellationToken);
        return Ok(catalog);
    }
}
