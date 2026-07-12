using FluentAssertions;
using NSubstitute;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Customers.Queries.GetMyProfile;

namespace SigortaPro.Application.Tests.Features.Customers;

public class GetMyProfileQueryHandlerTests
{
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetMyProfileQueryHandler _handler;

    public GetMyProfileQueryHandlerTests()
    {
        _handler = new GetMyProfileQueryHandler(_customerRepository, _identityService, _currentUserService);
    }

    [Fact]
    public async Task Handle_Should_ReturnMaskedProfileWithEmail_When_CustomerExists()
    {
        var appUserId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var customer = CustomerTestData.CreateCustomer(appUserId, customerId);
        customer.Vehicles.Add(CustomerTestData.CreateVehicle(customerId, Guid.NewGuid()));

        _currentUserService.UserId.Returns(appUserId);
        _customerRepository.GetProfileByAppUserIdAsync(appUserId, Arg.Any<CancellationToken>()).Returns(customer);
        _identityService.GetByIdAsync(appUserId, Arg.Any<CancellationToken>())
            .Returns(new IdentityUserInfo(appUserId, "ayse@ornek.com", new List<string> { "Customer" }));

        var result = await _handler.Handle(new GetMyProfileQuery(), CancellationToken.None);

        result.Id.Should().Be(customerId);
        result.Email.Should().Be("ayse@ornek.com");
        result.Vehicles.Should().HaveCount(1);
        // Ham TCKN sızdırılmaz; yalnızca maskeli değer döner (CODING_STANDARDS.md §4.2).
        result.MaskedTckn.Should().Be("*********10");
        result.MaskedTckn.Should().NotBe("11111111110");
    }

    [Fact]
    public async Task Handle_Should_ThrowForbidden_When_UserIsNotAuthenticated()
    {
        _currentUserService.UserId.Returns((Guid?)null);

        var act = () => _handler.Handle(new GetMyProfileQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_CustomerDoesNotExist()
    {
        var appUserId = Guid.NewGuid();
        _currentUserService.UserId.Returns(appUserId);
        _customerRepository.GetProfileByAppUserIdAsync(appUserId, Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.Customer?)null);

        var act = () => _handler.Handle(new GetMyProfileQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
