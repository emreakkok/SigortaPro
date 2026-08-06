using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Vehicles.DTOs;

namespace SigortaPro.Application.Features.Vehicles.Queries.GetVehicleCatalog;

// Katalog In-Memory cache'li Singleton provider'dan gelir; handler yalnızca CQRS yüzeyine adapte eder.
public sealed class GetVehicleCatalogQueryHandler : IQueryHandler<GetVehicleCatalogQuery, VehicleCatalogDto>
{
    private readonly IVehicleCatalogProvider _vehicleCatalogProvider;

    public GetVehicleCatalogQueryHandler(IVehicleCatalogProvider vehicleCatalogProvider)
    {
        _vehicleCatalogProvider = vehicleCatalogProvider;
    }

    public Task<VehicleCatalogDto> Handle(GetVehicleCatalogQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(_vehicleCatalogProvider.GetCatalog());
}
