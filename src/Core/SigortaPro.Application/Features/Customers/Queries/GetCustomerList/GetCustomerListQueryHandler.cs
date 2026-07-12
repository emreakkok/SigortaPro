using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Customers.DTOs;

namespace SigortaPro.Application.Features.Customers.Queries.GetCustomerList;

public sealed class GetCustomerListQueryHandler : IQueryHandler<GetCustomerListQuery, PagedResult<CustomerSummaryDto>>
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomerListQueryHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<PagedResult<CustomerSummaryDto>> Handle(GetCustomerListQuery request, CancellationToken cancellationToken)
    {
        // PaginationParams, sınır dışı sayfa boyutlarını kendi içinde normalleştirir (varsayılan 20, maks. 100).
        var paging = new PaginationParams { Page = request.Page, PageSize = request.PageSize };

        var page = await _customerRepository.SearchAsync(request.SearchTerm, request.City, paging, cancellationToken);

        var items = page.Items.Select(customer => customer.ToSummaryDto()).ToList();

        return new PagedResult<CustomerSummaryDto>(items, page.Page, page.PageSize, page.TotalCount);
    }
}
