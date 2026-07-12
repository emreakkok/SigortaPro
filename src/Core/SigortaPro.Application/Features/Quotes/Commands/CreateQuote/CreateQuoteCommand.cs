using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Quotes.DTOs;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Quotes.Commands.CreateQuote;

// Oturum sahibi müşteri, branş + risk objesi + teminat paketi seçerek teklif oluşturur.
// Fiyatlama motoru çağrılır ve teklif Priced durumunda (geçerlilik süresiyle) döner.
public sealed record CreateQuoteCommand(
    InsuranceBranch Branch,
    Guid? VehicleId,
    Guid? PropertyId,
    CoveragePackage CoveragePackage) : ICommand<QuoteDto>;
