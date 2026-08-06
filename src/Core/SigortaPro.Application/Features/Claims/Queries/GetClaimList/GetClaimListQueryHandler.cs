using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Claims.DTOs;
using SigortaPro.Application.Features.Quotes;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Claims.Queries.GetClaimList;

public sealed class GetClaimListQueryHandler : IQueryHandler<GetClaimListQuery, PagedResult<ClaimSummaryDto>>
{
    private readonly IClaimRepository _claimRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetClaimListQueryHandler(
        IClaimRepository claimRepository,
        ICustomerRepository customerRepository,
        ICurrentUserService currentUserService)
    {
        _claimRepository = claimRepository;
        _customerRepository = customerRepository;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<ClaimSummaryDto>> Handle(GetClaimListQuery request, CancellationToken cancellationToken)
    {
        // Acente personeli tüm hasarları görür; müşteri yalnızca kendi hasarlarını (customerId filtresi).
        // Sahiplik/personel muafiyeti mantığı QuoteAuthorization ile ortaktır ('dan yeniden kullanım).
        Guid? customerFilter = null;
        if (!QuoteAuthorization.IsStaff(_currentUserService))
        {
            var appUserId = _currentUserService.UserId
                ?? throw new ForbiddenAccessException();

            var customer = await _customerRepository.GetTrackedByAppUserIdAsync(appUserId, cancellationToken)
                ?? throw new NotFoundException(nameof(Customer), appUserId);

            customerFilter = customer.Id;
        }

        var paging = new PaginationParams { Page = request.Page, PageSize = request.PageSize };

        var page = await _claimRepository.SearchAsync(
            customerFilter, request.Status, request.PolicyId, paging, cancellationToken);

        var items = page.Items.Select(ClaimMappings.ToSummaryDto).ToList();

        return new PagedResult<ClaimSummaryDto>(items, page.Page, page.PageSize, page.TotalCount);
    }
}
