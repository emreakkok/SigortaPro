using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Cities.DTOs;

namespace SigortaPro.Application.Features.Cities.Queries.GetCityCatalog;

// İl kataloğunu döndüren salt okunur sorgu (girdi almaz). Adres formu combobox verisi.
public sealed record GetCityCatalogQuery : IQuery<CityCatalogDto>;
