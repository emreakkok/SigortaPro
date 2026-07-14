using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Policies.DTOs;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Policies.Queries.GetMyPolicies;

public sealed class GetMyPoliciesQueryHandler
    : IQueryHandler<GetMyPoliciesQuery, PagedResult<PolicyListItemDto>>
{
    private readonly IPolicyRepository _policyRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetMyPoliciesQueryHandler(
        IPolicyRepository policyRepository,
        ICustomerRepository customerRepository,
        ICurrentUserService currentUserService)
    {
        _policyRepository = policyRepository;
        _customerRepository = customerRepository;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<PolicyListItemDto>> Handle(
        GetMyPoliciesQuery request, CancellationToken cancellationToken)
    {
        // "Poliçelerim" yalnızca oturum sahibi müşterinin poliçelerini döndürür (kaynak sahipliği).
        var appUserId = _currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var customer = await _customerRepository.GetTrackedByAppUserIdAsync(appUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), appUserId);

        var paging = new PaginationParams { Page = request.Page, PageSize = request.PageSize };

        var page = await _policyRepository.GetByCustomerPagedAsync(
            customer.Id, request.Status, paging, cancellationToken);

        var items = page.Items.Select(PolicyMappings.ToListItem).ToList();

        return new PagedResult<PolicyListItemDto>(items, page.Page, page.PageSize, page.TotalCount);
    }
}
