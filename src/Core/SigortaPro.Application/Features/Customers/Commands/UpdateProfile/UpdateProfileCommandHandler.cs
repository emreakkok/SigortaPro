using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Customers.DTOs;
using SigortaPro.Domain.Common;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Customers.Commands.UpdateProfile;

public sealed class UpdateProfileCommandHandler : ICommandHandler<UpdateProfileCommand, CustomerDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateProfileCommandHandler> _logger;

    public UpdateProfileCommandHandler(
        ICustomerRepository customerRepository,
        IIdentityService identityService,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<UpdateProfileCommandHandler> logger)
    {
        _customerRepository = customerRepository;
        _identityService = identityService;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CustomerDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var appUserId = _currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var customer = await _customerRepository.GetTrackedByAppUserIdAsync(appUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), appUserId);

        customer.UpdateName(request.FirstName, request.LastName);
        customer.UpdateContactInfo(
            request.PhoneNumber,
            new Address(request.City, request.District, request.Neighborhood, request.PostalCode));

        _customerRepository.Update(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Müşteri profili güncellendi. CustomerId: {CustomerId}", customer.Id);

        // Yanıtın risk objelerini de içermesi için profil, risk objeleriyle birlikte yeniden okunur
        // (izlemeli çözümleme metodu bilinçli olarak yalındır; include taşımaz).
        var profile = await _customerRepository.GetProfileByAppUserIdAsync(appUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), appUserId);

        var user = await _identityService.GetByIdAsync(customer.AppUserId, cancellationToken);
        return profile.ToDto(user?.Email);
    }
}
