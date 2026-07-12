using SigortaPro.Application.Features.Renewals.DTOs;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Renewals;

// Entity → DTO manuel eşlemeleri (AutoMapper kullanılmaz — CODING_STANDARDS.md §4.2).
internal static class RenewalMappings
{
    public static RenewalDto ToDto(Renewal renewal) => new(
        renewal.Id,
        renewal.PolicyId,
        renewal.Policy?.PolicyNumber ?? string.Empty,
        renewal.NewQuoteId,
        renewal.NewQuote?.Branch ?? default,
        renewal.NewQuote?.Status ?? QuoteStatus.Draft,
        renewal.NewQuote?.TotalPremium ?? 0m,
        renewal.NewQuote?.ValidUntil,
        renewal.OfferedAt,
        renewal.IsAccepted,
        renewal.AcceptedAt);
}
