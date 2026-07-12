using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SigortaPro.Application.Common.Authorization;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Customers.Commands.AddProperty;
using SigortaPro.Application.Features.Customers.Commands.AddVehicle;
using SigortaPro.Application.Features.Customers.Commands.UpdateProfile;
using SigortaPro.Application.Features.Customers.Commands.UpdateVehicle;
using SigortaPro.Application.Features.Customers.DTOs;
using SigortaPro.Application.Features.Customers.Queries.GetCustomerById;
using SigortaPro.Application.Features.Customers.Queries.GetCustomerList;
using SigortaPro.Application.Features.Customers.Queries.GetMyProfile;

namespace SigortaPro.WebAPI.Controllers.v1;

[ApiController]
[Route("api/v1/customers")]
[Authorize]
public sealed class CustomersController : ControllerBase
{
    private readonly ISender _sender;

    public CustomersController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Oturum sahibi müşterinin kendi profilini (araç/konut risk objeleriyle) getirir.</summary>
    [HttpGet("me")]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMyProfileQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Oturum sahibi müşterinin ad/soyad, telefon ve adres bilgilerini günceller.</summary>
    [HttpPut("me")]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMyProfile(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Oturum sahibi müşterinin profiline yeni bir araç ekler.</summary>
    [HttpPost("me/vehicles")]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddVehicle(AddVehicleCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetMyProfile), result);
    }

    /// <summary>Oturum sahibi müşterinin belirtilen aracının bilgilerini günceller.</summary>
    [HttpPut("me/vehicles/{vehicleId:guid}")]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateVehicle(
        Guid vehicleId,
        UpdateVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateVehicleCommand(
            vehicleId,
            request.PlateNumber,
            request.Brand,
            request.Model,
            request.ManufactureYear,
            request.EnginePowerHp);

        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>Oturum sahibi müşterinin profiline yeni bir konut ekler.</summary>
    [HttpPost("me/properties")]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType(typeof(PropertyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddProperty(AddPropertyCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetMyProfile), result);
    }

    /// <summary>Müşteri listesini sayfalama, arama (ad/soyad/TCKN) ve il filtresiyle getirir (acente personeli).</summary>
    [HttpGet]
    [Authorize(Roles = Roles.Staff)]
    [ProducesResponseType(typeof(PagedResult<CustomerSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] GetCustomerListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Belirtilen müşterinin profil detayını getirir (acente personeli).</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = Roles.Staff)]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomerById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCustomerByIdQuery(id), cancellationToken);
        return Ok(result);
    }
}

// Araç güncelleme istek gövdesi; araç kimliği route'tan alınır (gövdede tekrar edilmez).
public sealed record UpdateVehicleRequest(
    string PlateNumber,
    string Brand,
    string Model,
    int ManufactureYear,
    int EnginePowerHp);
