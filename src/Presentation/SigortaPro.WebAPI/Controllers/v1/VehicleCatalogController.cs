using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SigortaPro.Application.Features.Vehicles.DTOs;
using SigortaPro.Application.Features.Vehicles.Queries.GetVehicleCatalog;

namespace SigortaPro.WebAPI.Controllers.v1;

// : Araç marka/model kataloğu — frontend'in cascading select (aranabilir combobox) verisi.
// Salt okunur referans veri; kimliği doğrulanmış her kullanıcı erişebilir.
[ApiController]
[Route("api/v1/vehicle-catalog")]
[Authorize]
public sealed class VehicleCatalogController : ControllerBase
{
    private readonly ISender _sender;

    public VehicleCatalogController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Araç marka ve modellerinin kataloğunu getirir (cascading select için).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(VehicleCatalogDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCatalog(CancellationToken cancellationToken)
    {
        var catalog = await _sender.Send(new GetVehicleCatalogQuery(), cancellationToken);
        return Ok(catalog);
    }
}
