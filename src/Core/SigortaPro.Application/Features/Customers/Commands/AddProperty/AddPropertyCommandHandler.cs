using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Authorization;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Pricing;
using SigortaPro.Application.Features.Customers.DTOs;
using SigortaPro.Domain.Common;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Customers.Commands.AddProperty;

public sealed class AddPropertyCommandHandler : ICommandHandler<AddPropertyCommand, PropertyDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IWriteRepository<Property> _propertyRepository;
    private readonly IEarthquakeZoneProvider _earthquakeZoneProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddPropertyCommandHandler> _logger;

    public AddPropertyCommandHandler(
        ICustomerRepository customerRepository,
        IWriteRepository<Property> propertyRepository,
        IEarthquakeZoneProvider earthquakeZoneProvider,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<AddPropertyCommandHandler> logger)
    {
        _customerRepository = customerRepository;
        _propertyRepository = propertyRepository;
        _earthquakeZoneProvider = earthquakeZoneProvider;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PropertyDto> Handle(AddPropertyCommand request, CancellationToken cancellationToken)
    {
        // Self-service (CustomerId null) → oturum sahibi müşteri; acente destekli (dolu) → hedef müşteri
        // (yalnızca personel — TargetCustomerResolver staff-only guard uygular).
        var customer = await TargetCustomerResolver.ResolveTrackedAsync(
            request.CustomerId, _currentUserService, _customerRepository, cancellationToken);

        var address = new Address(request.City, request.District, request.Neighborhood, request.PostalCode);

        // ADR-055: Deprem bölgesi kullanıcı beyanı değil, adresin İLİNDEN türetilir. İl tanınmıyorsa bölge
        // atanmaz (0) → fiyatlama motoru "bilinmeyen bölge" davranışını açık açıklamasıyla uygular;
        // sessizce yanlış (ve müşteri lehine) bir bölge atanmaz.
        var earthquakeZone = _earthquakeZoneProvider.ResolveZone(address.City) ?? EarthquakeZoneDefaults.Unknown;

        var property = new Property(
            customer.Id,
            address,
            request.BuildingAge,
            request.SquareMeters,
            earthquakeZone);

        await _propertyRepository.AddAsync(property, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Müşteriye konut eklendi. CustomerId: {CustomerId}, PropertyId: {PropertyId}",
            customer.Id, property.Id);

        return property.ToDto();
    }
}
