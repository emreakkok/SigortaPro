using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Customers.DTOs;

namespace SigortaPro.Application.Features.Customers.Commands.AddProperty;

// Oturum sahibi müşteri kendi profiline konut (Konut/DASK risk objesi) ekler.
public sealed record AddPropertyCommand(
    string City,
    string District,
    string Neighborhood,
    string PostalCode,
    int BuildingAge,
    int SquareMeters,
    int EarthquakeZone) : ICommand<PropertyDto>;
