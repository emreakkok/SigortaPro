using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Customers.DTOs;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Customers.Commands.UpdateVehicle;

public sealed class UpdateVehicleCommandHandler : ICommandHandler<UpdateVehicleCommand, VehicleDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IReadRepository<Vehicle> _vehicleReadRepository;
    private readonly IWriteRepository<Vehicle> _vehicleWriteRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateVehicleCommandHandler> _logger;

    public UpdateVehicleCommandHandler(
        ICustomerRepository customerRepository,
        IReadRepository<Vehicle> vehicleReadRepository,
        IWriteRepository<Vehicle> vehicleWriteRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<UpdateVehicleCommandHandler> logger)
    {
        _customerRepository = customerRepository;
        _vehicleReadRepository = vehicleReadRepository;
        _vehicleWriteRepository = vehicleWriteRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<VehicleDto> Handle(UpdateVehicleCommand request, CancellationToken cancellationToken)
    {
        var appUserId = _currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var customer = await _customerRepository.GetTrackedByAppUserIdAsync(appUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), appUserId);

        var vehicle = await _vehicleReadRepository.GetByIdAsync(request.VehicleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Vehicle), request.VehicleId);

        // Kaynak sahipliği kontrolü: müşteri yalnızca kendi aracını güncelleyebilir.
        if (vehicle.CustomerId != customer.Id)
        {
            throw new ForbiddenAccessException();
        }

        vehicle.UpdateDetails(
            request.PlateNumber,
            request.Brand,
            request.Model,
            request.ManufactureYear,
            request.EnginePowerHp,
            request.UsagePurpose);

        _vehicleWriteRepository.Update(vehicle);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Araç güncellendi. CustomerId: {CustomerId}, VehicleId: {VehicleId}",
            customer.Id, vehicle.Id);

        return vehicle.ToDto();
    }
}
