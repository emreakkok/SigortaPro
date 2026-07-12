namespace SigortaPro.Application.Features.Customers.DTOs;

public sealed record AddressDto(
    string City,
    string District,
    string Neighborhood,
    string PostalCode);
