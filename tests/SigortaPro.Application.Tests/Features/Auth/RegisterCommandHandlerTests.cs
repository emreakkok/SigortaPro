using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SigortaPro.Application.Common.Authorization;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Auth.Commands.Register;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Tests.Features.Auth;

public class RegisterCommandHandlerTests
{
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IRefreshTokenService _refreshTokenService = Substitute.For<IRefreshTokenService>();
    private readonly IWriteRepository<Customer> _customerRepository = Substitute.For<IWriteRepository<Customer>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        // ExecuteInTransactionAsync verilen operasyonu gerçekten çalıştırsın.
        _unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<Func<Task>>().Invoke());

        _handler = new RegisterCommandHandler(
            _identityService,
            _tokenService,
            _refreshTokenService,
            _customerRepository,
            _unitOfWork,
            Substitute.For<ILogger<RegisterCommandHandler>>());
    }

    private static RegisterCommand Command() => new(
        Email: "yeni@ornek.com",
        Password: "Gecerli!2345",
        FirstName: "Ahmet",
        LastName: "Yilmaz",
        Tckn: "10000000146",
        BirthDate: new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        PhoneNumber: "+905321234567",
        City: "Ankara",
        District: "Cankaya",
        Neighborhood: "Kizilay",
        PostalCode: "06420");

    [Fact]
    public async Task Handle_Should_ReturnFailure_When_EmailAlreadyExists()
    {
        _identityService.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _identityService.DidNotReceive().CreateUserAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_CreateCustomerUserAndReturnTokens_When_EmailIsNew()
    {
        var userId = Guid.NewGuid();
        _identityService.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _identityService.CreateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Roles.Customer, Arg.Any<CancellationToken>())
            .Returns(userId);
        _tokenService.CreateTokenPair(userId, Arg.Any<string>(), Arg.Any<IEnumerable<string>>())
            .Returns(new TokenPair("access-token", DateTime.UtcNow.AddMinutes(15), "refresh-token", DateTime.UtcNow.AddDays(7)));

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.UserId.Should().Be(userId);
        result.Value.Roles.Should().Contain(Roles.Customer);
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");

        await _customerRepository.Received(1).AddAsync(
            Arg.Is<Customer>(customer => customer.AppUserId == userId), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _refreshTokenService.Received(1).StoreAsync(
            userId, "refresh-token", Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }
}
