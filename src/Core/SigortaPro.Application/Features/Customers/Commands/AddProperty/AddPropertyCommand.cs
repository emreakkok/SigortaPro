using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Customers.DTOs;

namespace SigortaPro.Application.Features.Customers.Commands.AddProperty;

// Oturum sahibi müşteri kendi profiline konut (Konut/DASK risk objesi) ekler.
// ADR-055: Deprem bölgesi ARTIK KULLANICIDAN ALINMAZ — konutun ilinden sistem tarafından türetilir.
// Önceden serbestçe seçilebiliyordu ve primi %33'e varan oranda doğrudan etkilediğinden beyana açık bir
// fiyat manipülasyonu yüzeyiydi.
public sealed record AddPropertyCommand(
    string City,
    string District,
    string Neighborhood,
    string PostalCode,
    int BuildingAge,
    int SquareMeters) : ICommand<PropertyDto>;
