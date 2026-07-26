using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Vehicles.DTOs;

namespace SigortaPro.Application.Features.Vehicles.Queries.GetVehicleCatalog;

// Araç marka/model kataloğunu döndüren salt okunur sorgu (girdi almaz). Cascading select verisi.
public sealed record GetVehicleCatalogQuery : IQuery<VehicleCatalogDto>;
