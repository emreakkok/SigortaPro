using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Cities.DTOs;

namespace SigortaPro.Application.Features.Cities.Queries.GetCityCatalog;

// Katalog In-Memory cache'li Singleton provider'dan gelir; handler yalnızca CQRS yüzeyine adapte eder.
public sealed class GetCityCatalogQueryHandler : IQueryHandler<GetCityCatalogQuery, CityCatalogDto>
{
    private readonly ICityCatalogProvider _cityCatalogProvider;

    public GetCityCatalogQueryHandler(ICityCatalogProvider cityCatalogProvider)
    {
        _cityCatalogProvider = cityCatalogProvider;
    }

    public Task<CityCatalogDto> Handle(GetCityCatalogQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(_cityCatalogProvider.GetCatalog());
}
