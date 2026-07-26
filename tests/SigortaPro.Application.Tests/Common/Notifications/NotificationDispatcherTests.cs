using FluentAssertions;
using NSubstitute;
using SigortaPro.Application.Common.Authorization;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Notifications;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Tests.Common.Notifications;

public class NotificationDispatcherTests
{
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly INotificationRepository _repository = Substitute.For<INotificationRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IRealTimeNotifier _realTimeNotifier = Substitute.For<IRealTimeNotifier>();
    private readonly NotificationDispatcher _dispatcher;

    private static readonly RealTimeNotification Sample = new(
        "quote-created", NotificationSeverity.Info, "Yeni teklif", "Bir müşteri teklif oluşturdu.",
        Guid.NewGuid(), "Quote");

    public NotificationDispatcherTests()
    {
        _dispatcher = new NotificationDispatcher(_identityService, _repository, _unitOfWork, _realTimeNotifier);
    }

    [Fact]
    public async Task PublishToStaff_Should_PersistPerRecipientAndBroadcast_When_StaffExists()
    {
        var adminId = Guid.NewGuid();
        var personelId = Guid.NewGuid();
        _identityService.GetUserIdsInRoleAsync(Roles.Admin, Arg.Any<CancellationToken>())
            .Returns(new[] { adminId });
        _identityService.GetUserIdsInRoleAsync(Roles.Personel, Arg.Any<CancellationToken>())
            .Returns(new[] { personelId });

        await _dispatcher.PublishToStaffAsync(Sample, CancellationToken.None);

        // Alıcı başına bir kalıcı kayıt (fan-out) + tek SaveChanges + tek canlı yayın.
        await _repository.Received(2).AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).AddAsync(
            Arg.Is<Notification>(n => n.RecipientUserId == adminId && n.Type == "quote-created" && !n.IsRead),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _realTimeNotifier.Received(1).NotifyStaffAsync(Sample, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishToStaff_Should_NotDuplicate_When_UserHasBothRoles()
    {
        var userId = Guid.NewGuid();
        _identityService.GetUserIdsInRoleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new[] { userId });

        await _dispatcher.PublishToStaffAsync(Sample, CancellationToken.None);

        await _repository.Received(1).AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishToStaff_Should_StillBroadcast_When_NoStaffUsersFound()
    {
        _identityService.GetUserIdsInRoleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Guid>());

        await _dispatcher.PublishToStaffAsync(Sample, CancellationToken.None);

        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
        await _realTimeNotifier.Received(1).NotifyStaffAsync(Sample, Arg.Any<CancellationToken>());
    }
}
