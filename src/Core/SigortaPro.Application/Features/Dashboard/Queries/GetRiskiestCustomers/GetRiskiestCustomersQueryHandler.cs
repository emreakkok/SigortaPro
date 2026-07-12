using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Dashboard.DTOs;

namespace SigortaPro.Application.Features.Dashboard.Queries.GetRiskiestCustomers;

public sealed class GetRiskiestCustomersQueryHandler
    : IQueryHandler<GetRiskiestCustomersQuery, IReadOnlyList<CustomerRiskSegmentDto>>
{
    private readonly IDashboardRepository _dashboardRepository;

    public GetRiskiestCustomersQueryHandler(IDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }

    public async Task<IReadOnlyList<CustomerRiskSegmentDto>> Handle(
        GetRiskiestCustomersQuery request, CancellationToken cancellationToken)
    {
        var segments = await _dashboardRepository.GetRiskiestCustomerSegmentsAsync(request.Top, cancellationToken);

        return segments.Select(DashboardMappings.ToSegmentDto).ToList();
    }
}
