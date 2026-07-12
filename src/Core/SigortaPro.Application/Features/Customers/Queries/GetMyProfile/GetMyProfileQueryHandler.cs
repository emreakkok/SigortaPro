using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Customers.DTOs;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Customers.Queries.GetMyProfile;

public sealed class GetMyProfileQueryHandler : IQueryHandler<GetMyProfileQuery, CustomerDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;

    public GetMyProfileQueryHandler(
        ICustomerRepository customerRepository,
        IIdentityService identityService,
        ICurrentUserService currentUserService)
    {
        _customerRepository = customerRepository;
        _identityService = identityService;
        _currentUserService = currentUserService;
    }

    public async Task<CustomerDto> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var appUserId = _currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var customer = await _customerRepository.GetProfileByAppUserIdAsync(appUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), appUserId);

        var user = await _identityService.GetByIdAsync(customer.AppUserId, cancellationToken);

        return customer.ToDto(user?.Email);
    }
}
