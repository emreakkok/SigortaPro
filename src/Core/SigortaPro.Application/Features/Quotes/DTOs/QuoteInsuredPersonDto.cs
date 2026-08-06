namespace SigortaPro.Application.Features.Quotes.DTOs;

// "Başkası adına" sağlık teklifinde sigortalının özet görünümü.
// Ham TCKN taşınmaz; maskeli döner.
public sealed record QuoteInsuredPersonDto(
    string FullName,
    string MaskedTckn,
    DateTime BirthDate,
    string Relationship);
