using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Payments.DTOs;

// Satın alma sonucu oluşturulan poliçenin özeti.
public sealed record PolicySummaryDto(
    Guid Id,
    string PolicyNumber,
    PolicyStatus Status,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalPremium);
