namespace SigortaPro.Application.Features.Customers.DTOs;

// Admin müşteri listesi görünümü için özet DTO (az alan, TCKN maskeli).
public sealed record CustomerSummaryDto(
    Guid Id,
    string FirstName,
    string LastName,
    string MaskedTckn,
    string PhoneNumber,
    string City,
    DateTime CreatedAt);
