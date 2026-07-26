using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Quotes.DTOs;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Quotes.Queries.GetQuoteList;

public sealed class GetQuoteListQueryHandler : IQueryHandler<GetQuoteListQuery, PagedResult<QuoteSummaryDto>>
{
    private readonly IQuoteRepository _quoteRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetQuoteListQueryHandler(
        IQuoteRepository quoteRepository,
        ICustomerRepository customerRepository,
        ICurrentUserService currentUserService)
    {
        _quoteRepository = quoteRepository;
        _customerRepository = customerRepository;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<QuoteSummaryDto>> Handle(GetQuoteListQuery request, CancellationToken cancellationToken)
    {
        // Acente personeli tüm teklifleri görür; müşteri yalnızca kendi tekliflerini (customerId filtresi).
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

        var page = await _quoteRepository.SearchAsync(
            customerFilter, request.Status, request.Branch, request.Search, paging, cancellationToken);

        var items = page.Items.Select(QuoteMappings.ToSummaryDto).ToList();

        return new PagedResult<QuoteSummaryDto>(items, page.Page, page.PageSize, page.TotalCount);
    }
}
