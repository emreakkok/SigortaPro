using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Customers.Commands.AddVehicle;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Tests.Features.Customers;

public class AddVehicleCommandHandlerTests
{
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IWriteRepository<Vehicle> _vehicleRepository = Substitute.For<IWriteRepository<Vehicle>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly AddVehicleCommandHandler _handler;

    public AddVehicleCommandHandlerTests()
    {
        _handler = new AddVehicleCommandHandler(
            _customerRepository,
            _vehicleRepository,
            _currentUserService,
            _unitOfWork,
            Substitute.For<ILogger<AddVehicleCommandHandler>>());
    }

    private static AddVehicleCommand Command() => new(
        PlateNumber: "34 XYZ 456",
        Brand: "Honda",
        Model: "Civic",
        ManufactureYear: 2021,
        EnginePowerHp: 125);

    [Fact]
    public async Task Handle_Should_AddVehicleForCurrentCustomer_When_CustomerExists()
    {
        var appUserId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var customer = CustomerTestData.CreateCustomer(appUserId, customerId);

        _currentUserService.UserId.Returns(appUserId);
        _customerRepository.GetTrackedByAppUserIdAsync(appUserId, Arg.Any<CancellationToken>()).Returns(customer);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.PlateNumber.Should().Be("34 XYZ 456");
        await _vehicleRepository.Received(1).AddAsync(
            Arg.Is<Vehicle>(vehicle => vehicle.CustomerId == customerId), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_CustomerDoesNotExist()
    {
        var appUserId = Guid.NewGuid();
        _currentUserService.UserId.Returns(appUserId);
        _customerRepository.GetTrackedByAppUserIdAsync(appUserId, Arg.Any<CancellationToken>())
            .Returns((Customer?)null);

        var act = () => _handler.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _vehicleRepository.DidNotReceive().AddAsync(Arg.Any<Vehicle>(), Arg.Any<CancellationToken>());
    }
}
