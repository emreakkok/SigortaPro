using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Dashboard.DTOs;

namespace SigortaPro.Application.Features.Dashboard.Queries.GetPaymentReport;

public sealed class GetPaymentReportQueryHandler
    : IQueryHandler<GetPaymentReportQuery, PagedResult<PaymentReportItemDto>>
{
    private readonly IDashboardRepository _dashboardRepository;

    public GetPaymentReportQueryHandler(IDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }

    public async Task<PagedResult<PaymentReportItemDto>> Handle(
        GetPaymentReportQuery request, CancellationToken cancellationToken)
    {
        var paging = new PaginationParams { Page = request.Page, PageSize = request.PageSize };

        var page = await _dashboardRepository.GetPaymentsByDateRangeAsync(
            request.From, request.To, paging, cancellationToken);

        var items = page.Items.Select(DashboardMappings.ToReportItem).ToList();

        return new PagedResult<PaymentReportItemDto>(items, page.Page, page.PageSize, page.TotalCount);
    }
}
