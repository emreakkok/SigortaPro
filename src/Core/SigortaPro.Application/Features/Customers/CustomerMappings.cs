using SigortaPro.Application.Common.Security;
using SigortaPro.Application.Features.Customers.DTOs;
using SigortaPro.Domain.Common;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Customers;

// Entity → DTO manuel eşlemeleri (AutoMapper kullanılmaz — CODING_STANDARDS.md §4.2).
internal static class CustomerMappings
{
    public static CustomerDto ToDto(this Customer customer, string? email) => new(
        customer.Id,
        customer.FirstName,
        customer.LastName,
        SensitiveDataMasker.MaskTckn(customer.Tckn),
        customer.BirthDate,
        customer.PhoneNumber,
        email,
        customer.Address.ToDto(),
        customer.Vehicles.Select(vehicle => vehicle.ToDto()).ToList(),
        customer.Properties.Select(property => property.ToDto()).ToList());

    public static CustomerSummaryDto ToSummaryDto(this Customer customer) => new(
        customer.Id,
        customer.FirstName,
        customer.LastName,
        SensitiveDataMasker.MaskTckn(customer.Tckn),
        customer.PhoneNumber,
        customer.Address.City,
        customer.CreatedAt);

    public static VehicleDto ToDto(this Vehicle vehicle) => new(
        vehicle.Id,
        vehicle.PlateNumber,
        vehicle.Brand,
        vehicle.Model,
        vehicle.ManufactureYear,
        vehicle.EnginePowerHp);

    public static PropertyDto ToDto(this Property property) => new(
        property.Id,
        property.Address.ToDto(),
        property.BuildingAge,
        property.SquareMeters,
        property.EarthquakeZone);

    public static AddressDto ToDto(this Address address) => new(
        address.City,
        address.District,
        address.Neighborhood,
        address.PostalCode);
}
