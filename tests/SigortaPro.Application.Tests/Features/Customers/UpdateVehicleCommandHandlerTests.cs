using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Customers.Commands.UpdateVehicle;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Tests.Features.Customers;

public class UpdateVehicleCommandHandlerTests
{
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IReadRepository<Vehicle> _vehicleReadRepository = Substitute.For<IReadRepository<Vehicle>>();
    private readonly IWriteRepository<Vehicle> _vehicleWriteRepository = Substitute.For<IWriteRepository<Vehicle>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateVehicleCommandHandler _handler;

    public UpdateVehicleCommandHandlerTests()
    {
        _handler = new UpdateVehicleCommandHandler(
            _customerRepository,
            _vehicleReadRepository,
            _vehicleWriteRepository,
            _currentUserService,
            _unitOfWork,
            Substitute.For<ILogger<UpdateVehicleCommandHandler>>());
    }

    private static UpdateVehicleCommand Command(Guid vehicleId) => new(
        VehicleId: vehicleId,
        PlateNumber: "06 ABC 789",
        Brand: "Renault",
        Model: "Clio",
        ManufactureYear: 2020,
        EnginePowerHp: 90);

    [Fact]
    public async Task Handle_Should_UpdateVehicle_When_CustomerOwnsIt()
    {
        var appUserId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var customer = CustomerTestData.CreateCustomer(appUserId, customerId);
        var vehicle = CustomerTestData.CreateVehicle(customerId, vehicleId);

        _currentUserService.UserId.Returns(appUserId);
        _customerRepository.GetTrackedByAppUserIdAsync(appUserId, Arg.Any<CancellationToken>()).Returns(customer);
        _vehicleReadRepository.GetByIdAsync(vehicleId, Arg.Any<CancellationToken>()).Returns(vehicle);

        var result = await _handler.Handle(Command(vehicleId), CancellationToken.None);

        result.PlateNumber.Should().Be("06 ABC 789");
        vehicle.Brand.Should().Be("Renault");
        _vehicleWriteRepository.Received(1).Update(vehicle);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ThrowForbidden_When_VehicleBelongsToAnotherCustomer()
    {
        var appUserId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var customer = CustomerTestData.CreateCustomer(appUserId, customerId);
        // Araç başka bir müşteriye ait — kaynak sahipliği kontrolü tetiklenmeli.
        var vehicle = CustomerTestData.CreateVehicle(otherCustomerId, vehicleId);

        _currentUserService.UserId.Returns(appUserId);
        _customerRepository.GetTrackedByAppUserIdAsync(appUserId, Arg.Any<CancellationToken>()).Returns(customer);
        _vehicleReadRepository.GetByIdAsync(vehicleId, Arg.Any<CancellationToken>()).Returns(vehicle);

        var act = () => _handler.Handle(Command(vehicleId), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        _vehicleWriteRepository.DidNotReceive().Update(Arg.Any<Vehicle>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_VehicleDoesNotExist()
    {
        var appUserId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var customer = CustomerTestData.CreateCustomer(appUserId, customerId);

        _currentUserService.UserId.Returns(appUserId);
        _customerRepository.GetTrackedByAppUserIdAsync(appUserId, Arg.Any<CancellationToken>()).Returns(customer);
        _vehicleReadRepository.GetByIdAsync(vehicleId, Arg.Any<CancellationToken>()).Returns((Vehicle?)null);

        var act = () => _handler.Handle(Command(vehicleId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
