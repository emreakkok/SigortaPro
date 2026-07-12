using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Dashboard.DTOs;

namespace SigortaPro.Application.Features.Dashboard.Queries.GetDashboardSummary;

// Admin paneli özet metrikleri (parametre yok; tüm metrikler tek çağrıda döner).
public sealed record GetDashboardSummaryQuery : IQuery<DashboardSummaryDto>;
