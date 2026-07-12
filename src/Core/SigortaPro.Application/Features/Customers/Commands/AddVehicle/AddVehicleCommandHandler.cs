using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Customers.DTOs;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Customers.Commands.AddVehicle;

public sealed class AddVehicleCommandHandler : ICommandHandler<AddVehicleCommand, VehicleDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IWriteRepository<Vehicle> _vehicleRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddVehicleCommandHandler> _logger;

    public AddVehicleCommandHandler(
        ICustomerRepository customerRepository,
        IWriteRepository<Vehicle> vehicleRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<AddVehicleCommandHandler> logger)
    {
        _customerRepository = customerRepository;
        _vehicleRepository = vehicleRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<VehicleDto> Handle(AddVehicleCommand request, CancellationToken cancellationToken)
    {
        var appUserId = _currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var customer = await _customerRepository.GetTrackedByAppUserIdAsync(appUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), appUserId);

        var vehicle = new Vehicle(
            customer.Id,
            request.PlateNumber,
            request.Brand,
            request.Model,
            request.ManufactureYear,
            request.EnginePowerHp);

        await _vehicleRepository.AddAsync(vehicle, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Müşteriye araç eklendi. CustomerId: {CustomerId}, VehicleId: {VehicleId}",
            customer.Id, vehicle.Id);

        return vehicle.ToDto();
    }
}
