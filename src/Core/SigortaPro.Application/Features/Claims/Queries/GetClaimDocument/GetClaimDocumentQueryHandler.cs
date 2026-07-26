using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Claims.DTOs;
using SigortaPro.Application.Features.Quotes;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Claims.Queries.GetClaimDocument;

public sealed class GetClaimDocumentQueryHandler : IQueryHandler<GetClaimDocumentQuery, ClaimDocumentContentDto>
{
    private readonly IClaimRepository _claimRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorageService _fileStorage;

    public GetClaimDocumentQueryHandler(
        IClaimRepository claimRepository,
        ICustomerRepository customerRepository,
        ICurrentUserService currentUserService,
        IFileStorageService fileStorage)
    {
        _claimRepository = claimRepository;
        _customerRepository = customerRepository;
        _currentUserService = currentUserService;
        _fileStorage = fileStorage;
    }

    public async Task<ClaimDocumentContentDto> Handle(GetClaimDocumentQuery request, CancellationToken cancellationToken)
    {
        var claim = await _claimRepository.GetDetailByIdAsync(request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(Claim), request.ClaimId);

        // Kaynak sahipliği: müşteri yalnızca kendi hasarının belgesine erişir; Admin/Personel muaf (QuoteAuthorization).
        await QuoteAuthorization.EnsureCanAccessAsync(
            claim.CustomerId, _currentUserService, _customerRepository, cancellationToken);

        var document = claim.Documents.FirstOrDefault(item => item.Id == request.DocumentId)
            ?? throw new NotFoundException(nameof(ClaimDocument), request.DocumentId);

        var content = await _fileStorage.ReadAsync(document.StorageKey, cancellationToken)
            ?? throw new NotFoundException(nameof(ClaimDocument), request.DocumentId);

        return new ClaimDocumentContentDto(content, document.ContentType, document.FileName);
    }
}
