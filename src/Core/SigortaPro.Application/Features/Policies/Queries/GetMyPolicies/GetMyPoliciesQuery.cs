using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Policies.DTOs;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Policies.Queries.GetMyPolicies;

// Oturum sahibi müşterinin poliçeleri (en yeni başlangıç önce, sayfalanmış). Durum filtresi opsiyonel
// (aktif/pasif sekmeleri için). Yalnızca müşterinin kendi poliçeleri döner (kaynak sahipliği).
public sealed record GetMyPoliciesQuery(
    int Page = 1,
    int PageSize = 20,
    PolicyStatus? Status = null) : IQuery<PagedResult<PolicyListItemDto>>;
