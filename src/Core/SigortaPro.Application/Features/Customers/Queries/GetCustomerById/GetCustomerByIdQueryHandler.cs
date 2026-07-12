using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Customers.DTOs;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Customers.Queries.GetCustomerById;

public sealed class GetCustomerByIdQueryHandler : IQueryHandler<GetCustomerByIdQuery, CustomerDto>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IIdentityService _identityService;

    public GetCustomerByIdQueryHandler(
        ICustomerRepository customerRepository,
        IIdentityService identityService)
    {
        _customerRepository = customerRepository;
        _identityService = identityService;
    }

    public async Task<CustomerDto> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetProfileByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);

        var user = await _identityService.GetByIdAsync(customer.AppUserId, cancellationToken);

        return customer.ToDto(user?.Email);
    }
}
