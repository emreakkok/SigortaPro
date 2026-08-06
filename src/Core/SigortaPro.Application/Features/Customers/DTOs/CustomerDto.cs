namespace SigortaPro.Application.Features.Customers.DTOs;

// Müşteri profil detayı. Hassas alan (ham TCKN) taşımaz; TCKN maskeli döner.
public sealed record CustomerDto(
    Guid Id,
    string FirstName,
    string LastName,
    string MaskedTckn,
    DateTime BirthDate,
    string PhoneNumber,
    string? Email,
    AddressDto Address,
    IReadOnlyList<VehicleDto> Vehicles,
    IReadOnlyList<PropertyDto> Properties);
