using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Renewals.DTOs;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Renewals.Queries.GetMyRenewals;

public sealed class GetMyRenewalsQueryHandler : IQueryHandler<GetMyRenewalsQuery, PagedResult<RenewalDto>>
{
    private readonly IRenewalRepository _renewalRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetMyRenewalsQueryHandler(
        IRenewalRepository renewalRepository,
        ICustomerRepository customerRepository,
        ICurrentUserService currentUserService)
    {
        _renewalRepository = renewalRepository;
        _customerRepository = customerRepository;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<RenewalDto>> Handle(GetMyRenewalsQuery request, CancellationToken cancellationToken)
    {
        var appUserId = _currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var customer = await _customerRepository.GetTrackedByAppUserIdAsync(appUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), appUserId);

        var paging = new PaginationParams { Page = request.Page, PageSize = request.PageSize };

        var page = await _renewalRepository.GetByCustomerAsync(customer.Id, paging, cancellationToken);

        var items = page.Items.Select(RenewalMappings.ToDto).ToList();

        return new PagedResult<RenewalDto>(items, page.Page, page.PageSize, page.TotalCount);
    }
}
