using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
