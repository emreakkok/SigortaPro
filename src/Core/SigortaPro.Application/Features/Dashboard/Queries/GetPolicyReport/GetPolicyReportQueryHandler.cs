using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Dashboard.DTOs;

namespace SigortaPro.Application.Features.Dashboard.Queries.GetPolicyReport;

public sealed class GetPolicyReportQueryHandler
    : IQueryHandler<GetPolicyReportQuery, PagedResult<PolicyReportItemDto>>
{
    private readonly IDashboardRepository _dashboardRepository;

    public GetPolicyReportQueryHandler(IDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }

    public async Task<PagedResult<PolicyReportItemDto>> Handle(
        GetPolicyReportQuery request, CancellationToken cancellationToken)
    {
        var paging = new PaginationParams { Page = request.Page, PageSize = request.PageSize };

        var page = await _dashboardRepository.GetPoliciesByDateRangeAsync(
            request.From, request.To, paging, cancellationToken);

        var items = page.Items.Select(DashboardMappings.ToReportItem).ToList();

        return new PagedResult<PolicyReportItemDto>(items, page.Page, page.PageSize, page.TotalCount);
    }
}
