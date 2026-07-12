using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Customers.Commands.UpdateProfile;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Tests.Features.Customers;

public class UpdateProfileCommandHandlerTests
{
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateProfileCommandHandler _handler;

    public UpdateProfileCommandHandlerTests()
    {
        _handler = new UpdateProfileCommandHandler(
            _customerRepository,
            _identityService,
            _currentUserService,
            _unitOfWork,
            Substitute.For<ILogger<UpdateProfileCommandHandler>>());
    }

    private static UpdateProfileCommand Command() => new(
        FirstName: "Ayşe",
        LastName: "Kaya",
        PhoneNumber: "+905321234567",
        City: "Ankara",
        District: "Çankaya",
        Neighborhood: "Kızılay",
        PostalCode: "06420");

    [Fact]
    public async Task Handle_Should_UpdateCustomerAndPersist_When_CustomerExists()
    {
        var appUserId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var customer = CustomerTestData.CreateCustomer(appUserId, customerId);

        _currentUserService.UserId.Returns(appUserId);
        _customerRepository.GetTrackedByAppUserIdAsync(appUserId, Arg.Any<CancellationToken>()).Returns(customer);
        _customerRepository.GetProfileByAppUserIdAsync(appUserId, Arg.Any<CancellationToken>()).Returns(customer);
        _identityService.GetByIdAsync(appUserId, Arg.Any<CancellationToken>())
            .Returns(new IdentityUserInfo(appUserId, "ayse@ornek.com", new List<string> { "Customer" }));

        var result = await _handler.Handle(Command(), CancellationToken.None);

        customer.LastName.Should().Be("Kaya");
        customer.PhoneNumber.Should().Be("+905321234567");
        customer.Address.City.Should().Be("Ankara");
        result.LastName.Should().Be("Kaya");

        _customerRepository.Received(1).Update(Arg.Is<Customer>(c => c.Id == customerId));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
