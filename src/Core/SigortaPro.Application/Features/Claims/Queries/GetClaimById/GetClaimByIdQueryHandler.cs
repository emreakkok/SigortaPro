using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Claims.DTOs;
using SigortaPro.Application.Features.Quotes;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Claims.Queries.GetClaimById;

public sealed class GetClaimByIdQueryHandler : IQueryHandler<GetClaimByIdQuery, ClaimDto>
{
    private readonly IClaimRepository _claimRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetClaimByIdQueryHandler(
        IClaimRepository claimRepository,
        ICustomerRepository customerRepository,
        ICurrentUserService currentUserService)
    {
        _claimRepository = claimRepository;
        _customerRepository = customerRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ClaimDto> Handle(GetClaimByIdQuery request, CancellationToken cancellationToken)
    {
        var claim = await _claimRepository.GetDetailByIdAsync(request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(Claim), request.ClaimId);

        // Kaynak sahipliği: müşteri yalnızca kendi hasarına erişir; Admin/Personel muaf (QuoteAuthorization — Task 9).
        await QuoteAuthorization.EnsureCanAccessAsync(
            claim.CustomerId, _currentUserService, _customerRepository, cancellationToken);

        return ClaimMappings.ToDto(claim, claim.Policy?.PolicyNumber ?? string.Empty);
    }
}
