using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Dashboard.DTOs;

namespace SigortaPro.Application.Features.Dashboard.Queries.GetDashboardSummary;

// Operasyon dashboard'ının TEK çağrısı — tüm bloklar (dönem KPI'ları, aksiyon merkezi, prim serisi,
// satış hunisi, branş performansı, hasar operasyonu, portföy) tek istekte döner. Filtre değiştiğinde yalnızca
// bu sorgu yeniden çalışır (onlarca ayrı uç yoktur).
// From/To verilmezse varsayılan aralık son 30 gündür. Aralık kapsayıcıdır (inclusive).
public sealed record GetDashboardSummaryQuery(
    DateTime? From = null,
    DateTime? To = null) : IQuery<DashboardSummaryDto>;
