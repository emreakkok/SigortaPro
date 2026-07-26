using FluentAssertions;
using NSubstitute;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Vehicles.DTOs;
using SigortaPro.Application.Features.Vehicles.Queries.GetVehicleCatalog;

namespace SigortaPro.Application.Tests.Features.Vehicles;

public class GetVehicleCatalogQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_ReturnCatalogFromProvider()
    {
        var provider = Substitute.For<IVehicleCatalogProvider>();
        var expected = new VehicleCatalogDto(new[]
        {
            new VehicleBrandDto("Toyota", new[] { "Corolla", "Yaris" }),
        });
        provider.GetCatalog().Returns(expected);

        var handler = new GetVehicleCatalogQueryHandler(provider);

        var result = await handler.Handle(new GetVehicleCatalogQuery(), CancellationToken.None);

        result.Should().BeSameAs(expected);
        provider.Received(1).GetCatalog();
    }
}
