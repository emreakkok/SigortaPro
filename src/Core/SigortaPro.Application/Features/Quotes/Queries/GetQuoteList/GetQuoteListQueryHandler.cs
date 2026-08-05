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
        var isStaff = QuoteAuthorization.IsStaff(_currentUserService);
        if (!isStaff)
        {
            var appUserId = _currentUserService.UserId
                ?? throw new ForbiddenAccessException();

            var customer = await _customerRepository.GetTrackedByAppUserIdAsync(appUserId, cancellationToken)
                ?? throw new NotFoundException(nameof(Customer), appUserId);

            customerFilter = customer.Id;
        }

        // "Benim oluşturduklarım" yalnızca personel için anlamlıdır (müşteri zaten kendi tekliflerini görür).
        // Personel talep ederse, oturum sahibi personelin müşteri adına oluşturduğu tekliflere daraltılır.
        Guid? createdByStaffFilter = isStaff && request.CreatedByMe ? _currentUserService.UserId : null;

        var paging = new PaginationParams { Page = request.Page, PageSize = request.PageSize };

        var page = await _quoteRepository.SearchAsync(
            customerFilter, request.Status, request.Branch, request.Search, paging, createdByStaffFilter, cancellationToken);

        var items = page.Items.Select(QuoteMappings.ToSummaryDto).ToList();

        return new PagedResult<QuoteSummaryDto>(items, page.Page, page.PageSize, page.TotalCount);
    }
}
