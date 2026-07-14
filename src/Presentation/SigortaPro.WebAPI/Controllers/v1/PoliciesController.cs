using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SigortaPro.Application.Common.Authorization;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Policies.DTOs;
using SigortaPro.Application.Features.Policies.Queries.GetMyPolicies;
using SigortaPro.Application.Features.Policies.Queries.GetPolicyById;
using SigortaPro.Application.Features.Policies.Queries.GetPolicyDocument;

namespace SigortaPro.WebAPI.Controllers.v1;

[ApiController]
[Route("api/v1/policies")]
[Authorize]
public sealed class PoliciesController : ControllerBase
{
    private readonly ISender _sender;

    public PoliciesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Oturum sahibi müşterinin poliçelerini (aktif/pasif) sayfalı getirir ("Poliçelerim").</summary>
    [HttpGet]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType(typeof(PagedResult<PolicyListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyPolicies([FromQuery] GetMyPoliciesQuery query, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Poliçe detayını (teminat tablosu ile) getirir; sahiplik kontrollü (müşteri kendi poliçesi, personel muaf).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PolicyDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPolicyByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>Poliçe sertifikası PDF'ini indirir (sahiplik kontrolü ile; ilk erişimde üretilir).</summary>
    [HttpGet("{id:guid}/document")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocument(Guid id, CancellationToken cancellationToken)
    {
        var file = await _sender.Send(new GetPolicyDocumentQuery(id), cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
