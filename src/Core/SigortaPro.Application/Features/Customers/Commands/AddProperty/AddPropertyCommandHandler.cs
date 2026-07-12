using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Customers.DTOs;
using SigortaPro.Domain.Common;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Customers.Commands.AddProperty;

public sealed class AddPropertyCommandHandler : ICommandHandler<AddPropertyCommand, PropertyDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IWriteRepository<Property> _propertyRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddPropertyCommandHandler> _logger;

    public AddPropertyCommandHandler(
        ICustomerRepository customerRepository,
        IWriteRepository<Property> propertyRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<AddPropertyCommandHandler> logger)
    {
        _customerRepository = customerRepository;
        _propertyRepository = propertyRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PropertyDto> Handle(AddPropertyCommand request, CancellationToken cancellationToken)
    {
        var appUserId = _currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var customer = await _customerRepository.GetTrackedByAppUserIdAsync(appUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), appUserId);

        var address = new Address(request.City, request.District, request.Neighborhood, request.PostalCode);
        var property = new Property(
            customer.Id,
            address,
            request.BuildingAge,
            request.SquareMeters,
            request.EarthquakeZone);

        await _propertyRepository.AddAsync(property, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Müşteriye konut eklendi. CustomerId: {CustomerId}, PropertyId: {PropertyId}",
            customer.Id, property.Id);

        return property.ToDto();
    }
}
