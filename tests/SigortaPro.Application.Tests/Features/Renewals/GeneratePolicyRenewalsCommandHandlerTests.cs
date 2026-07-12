using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Notifications;
using SigortaPro.Application.Features.Renewals.Commands.GeneratePolicyRenewals;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Tests.Features.Renewals;

// Not: Yenileme üretiminin tam happy-path'i (fiyatlama + hasar çarpanı) navigation yüklü Policy/Quote gerektirdiğinden
// SQL Server Express üzerinde uçtan uca doğrulanmıştır (proje deseni). Buradaki testler orkestrasyon/guard davranışını kapsar.
public class GeneratePolicyRenewalsCommandHandlerTests
{
    private readonly IPolicyRepository _policyRepository = Substitute.For<IPolicyRepository>();
    private readonly IQuoteRepository _quoteRepository = Substitute.For<IQuoteRepository>();
    private readonly IRenewalRepository _renewalRepository = Substitute.For<IRenewalRepository>();
    private readonly IClaimRepository _claimRepository = Substitute.For<IClaimRepository>();
    private readonly IPricingEngine _pricingEngine = Substitute.For<IPricingEngine>();
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly GeneratePolicyRenewalsCommandHandler _handler;

    private readonly DateTime _now = new(2026, 7, 12, 0, 0, 0, DateTimeKind.Utc);

    public GeneratePolicyRenewalsCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(_now);
        _handler = new GeneratePolicyRenewalsCommandHandler(
            _policyRepository, _quoteRepository, _renewalRepository, _claimRepository, _pricingEngine,
            _notificationService, _dateTimeProvider, _unitOfWork,
            Substitute.For<ILogger<GeneratePolicyRenewalsCommandHandler>>());
    }

    [Fact]
    public async Task Handle_Should_ReturnZeroAndNotSave_When_NoPoliciesDueForRenewal()
    {
        _policyRepository.GetDueForRenewalAsync(_now, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Policy>());

        var count = await _handler.Handle(new GeneratePolicyRenewalsCommand(), CancellationToken.None);

        count.Should().Be(0);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _notificationService.DidNotReceive().NotifyRenewalOfferedAsync(
            Arg.Any<RenewalOfferedNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_SkipPolicyAndNotSave_When_RequiredNavigationDataMissing()
    {
        // Teklif/ürün/müşteri navigasyonu olmayan poliçe (veri tutarsızlığı) güvenle atlanır.
        var policy = new Policy("POL-2026-000009", Guid.NewGuid(), Guid.NewGuid(), _now, _now.AddDays(20), 20000m);
        _policyRepository.GetDueForRenewalAsync(_now, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[] { policy });

        var count = await _handler.Handle(new GeneratePolicyRenewalsCommand(), CancellationToken.None);

        count.Should().Be(0);
        await _quoteRepository.DidNotReceive().AddAsync(Arg.Any<Quote>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
