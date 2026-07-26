using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Claims.DTOs;

namespace SigortaPro.Application.Features.Claims.Queries.GetClaimDocument;

// Hasar belgesinin baytlarını indirir/görüntüler. Kaynak sahipliği kontrollüdür: müşteri yalnızca kendi
// hasarının belgesine, Admin/Personel tüm hasar belgelerine erişebilir (anonim erişemez — controller [Authorize]).
public sealed record GetClaimDocumentQuery(Guid ClaimId, Guid DocumentId) : IQuery<ClaimDocumentContentDto>;
