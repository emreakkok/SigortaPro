using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Policies.DTOs;

// "Poliçelerim" liste görünümü için özet DTO. Branş/ürün adı, poliçenin kaynaklandığı tekliften gelir.
public sealed record PolicyListItemDto(
    Guid Id,
    string PolicyNumber,
    InsuranceBranch Branch,
    string ProductName,
    PolicyStatus Status,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalPremium);
