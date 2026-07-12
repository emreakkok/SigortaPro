using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Payments.DTOs;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Payments.Queries.GetPaymentHistory;

public sealed class GetPaymentHistoryQueryHandler
    : IQueryHandler<GetPaymentHistoryQuery, PagedResult<PaymentHistoryItemDto>>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetPaymentHistoryQueryHandler(
        IPaymentRepository paymentRepository,
        ICustomerRepository customerRepository,
        ICurrentUserService currentUserService)
    {
        _paymentRepository = paymentRepository;
        _customerRepository = customerRepository;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<PaymentHistoryItemDto>> Handle(
        GetPaymentHistoryQuery request, CancellationToken cancellationToken)
    {
        var appUserId = _currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var customer = await _customerRepository.GetTrackedByAppUserIdAsync(appUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), appUserId);

        var paging = new PaginationParams { Page = request.Page, PageSize = request.PageSize };

        var page = await _paymentRepository.GetByCustomerAsync(customer.Id, paging, cancellationToken);

        var items = page.Items.Select(PaymentMappings.ToHistoryItem).ToList();

        return new PagedResult<PaymentHistoryItemDto>(items, page.Page, page.PageSize, page.TotalCount);
    }
}
