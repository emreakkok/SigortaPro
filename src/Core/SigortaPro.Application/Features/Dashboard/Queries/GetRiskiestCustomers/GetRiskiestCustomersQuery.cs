using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Dashboard.DTOs;

namespace SigortaPro.Application.Features.Dashboard.Queries.GetRiskiestCustomers;

// En riskli müşteri segmentleri (hasar sayısına göre ilk N). Küçük, sabit boyutlu liste — sayfalama gerektirmez.
public sealed record GetRiskiestCustomersQuery(int Top = 10) : IQuery<IReadOnlyList<CustomerRiskSegmentDto>>;
