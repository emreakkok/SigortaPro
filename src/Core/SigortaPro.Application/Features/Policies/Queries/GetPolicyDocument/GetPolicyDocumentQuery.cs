using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Policies.DTOs;

namespace SigortaPro.Application.Features.Policies.Queries.GetPolicyDocument;

// Poliçe sertifikası PDF'ini indirir. Belge yoksa ilk erişimde üretilip saklanır (ADR-023).
public sealed record GetPolicyDocumentQuery(Guid PolicyId) : IQuery<PolicyDocumentFileDto>;
