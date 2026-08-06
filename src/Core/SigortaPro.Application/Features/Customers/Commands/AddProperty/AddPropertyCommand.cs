using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Customers.DTOs;

namespace SigortaPro.Application.Features.Customers.Commands.AddProperty;

// Oturum sahibi müşteri kendi profiline konut (Konut/DASK risk objesi) ekler.
// Deprem bölgesi ARTIK KULLANICIDAN ALINMAZ — konutun ilinden sistem tarafından türetilir.
// Önceden serbestçe seçilebiliyordu ve primi %33'e varan oranda doğrudan etkilediğinden beyana açık bir
// fiyat manipülasyonu yüzeyiydi.
// CustomerId (acente destekli, additive): dolu ise konut bu müşteri ADINA eklenir ve çağıran yalnızca acente
// personeli olabilir (controller nested route'ta set eder). null = self-service (oturum sahibi müşteri).
public sealed record AddPropertyCommand(
    string City,
    string District,
    string Neighborhood,
    string PostalCode,
    int BuildingAge,
    int SquareMeters,
    Guid? CustomerId = null) : ICommand<PropertyDto>;
