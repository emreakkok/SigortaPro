using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Customers.DTOs;

namespace SigortaPro.Application.Features.Customers.Queries.GetMyProfile;

// Oturum sahibi müşterinin kendi profilini (risk objeleriyle birlikte) döner. Parametre almaz;
// müşteri ICurrentUserService üzerinden çözümlenir (kaynak sahipliği içkindir — DEVELOPMENT_RULES.md §7).
public sealed record GetMyProfileQuery : IQuery<CustomerDto>;
