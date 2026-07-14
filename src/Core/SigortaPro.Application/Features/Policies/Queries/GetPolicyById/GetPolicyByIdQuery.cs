using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Policies.DTOs;

namespace SigortaPro.Application.Features.Policies.Queries.GetPolicyById;

// Poliçe detayı (teminat tablosu ile). Sahiplik kontrollü: müşteri kendi poliçesini, Admin/Personel tümünü görür.
public sealed record GetPolicyByIdQuery(Guid PolicyId) : IQuery<PolicyDetailDto>;
