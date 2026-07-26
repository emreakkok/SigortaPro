using FluentAssertions;
using NSubstitute;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Cities.DTOs;
using SigortaPro.Application.Features.Cities.Queries.GetCityCatalog;

namespace SigortaPro.Application.Tests.Features.Cities;

public class GetCityCatalogQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_ReturnCatalogFromProvider()
    {
        var provider = Substitute.For<ICityCatalogProvider>();
        var expected = new CityCatalogDto(new[] { new CityDto("İstanbul"), new CityDto("Ankara") });
        provider.GetCatalog().Returns(expected);

        var handler = new GetCityCatalogQueryHandler(provider);

        var result = await handler.Handle(new GetCityCatalogQuery(), CancellationToken.None);

        result.Should().BeSameAs(expected);
        provider.Received(1).GetCatalog();
    }
}
