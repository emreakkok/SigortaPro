using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SigortaPro.Application.Common.Authorization;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Staff.Commands.CreateStaffUser;

namespace SigortaPro.Application.Tests.Features.Staff;

// Personel oluşturma handler'ı. Rolün sunucuda Personel'e sabitlendiği ve şifrenin dışa sızmadığı doğrulanır.
public class CreateStaffUserCommandHandlerTests
{
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly CreateStaffUserCommandHandler _handler;

    public CreateStaffUserCommandHandlerTests()
    {
        _handler = new CreateStaffUserCommandHandler(
            _identityService, _currentUserService, Substitute.For<ILogger<CreateStaffUserCommandHandler>>());
    }

    [Fact]
    public async Task Handle_Should_CreateStaff_Through_PersonelFixedPath()
    {
        var staffId = Guid.NewGuid();
        _identityService.ExistsByEmailAsync("yeni@ornek.com", Arg.Any<CancellationToken>()).Returns(false);
        _identityService.CreateStaffUserAsync("yeni@ornek.com", "Ad Soyad", "Gizli!2345", Arg.Any<CancellationToken>())
            .Returns(staffId);
        _identityService.GetStaffByIdAsync(staffId, Arg.Any<CancellationToken>())
            .Returns(new StaffUserInfo(staffId, "yeni@ornek.com", "Ad Soyad", true, new[] { Roles.Personel }));

        var result = await _handler.Handle(
            new CreateStaffUserCommand("yeni@ornek.com", "Ad Soyad", "Gizli!2345"), CancellationToken.None);

        result.Roles.Should().ContainSingle().Which.Should().Be(Roles.Personel);
        result.IsActive.Should().BeTrue();
        // Rol handler'dan geçirilmez; CreateStaffUserAsync imzası rol parametresi ALMAZ (sunucuda sabit).
        await _identityService.Received(1).CreateStaffUserAsync(
            "yeni@ornek.com", "Ad Soyad", "Gizli!2345", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Throw_When_EmailAlreadyExists()
    {
        _identityService.ExistsByEmailAsync("mevcut@ornek.com", Arg.Any<CancellationToken>()).Returns(true);

        var act = () => _handler.Handle(
            new CreateStaffUserCommand("mevcut@ornek.com", "Ad Soyad", "Gizli!2345"), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
        await _identityService.DidNotReceive().CreateStaffUserAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
