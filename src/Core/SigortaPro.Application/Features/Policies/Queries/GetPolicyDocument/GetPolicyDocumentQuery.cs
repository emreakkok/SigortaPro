using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Policies.DTOs;

namespace SigortaPro.Application.Features.Policies.Queries.GetPolicyDocument;

// Poliçe sertifikası PDF'ini indirir. Belge yoksa ilk erişimde üretilip saklanır.
public sealed record GetPolicyDocumentQuery(Guid PolicyId) : IQuery<PolicyDocumentFileDto>;
