using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SigortaPro.Application.Common.Authorization;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Renewals.Commands.AcceptRenewal;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Tests.Features.Renewals;

// Not: Onayın happy-path'i (yenileme kabul + teklif Approve) navigation yüklü NewQuote gerektirdiğinden
// SQL Server Express üzerinde uçtan uca doğrulanmıştır. Buradaki testler not-found guard davranışını kapsar.
public class AcceptRenewalCommandHandlerTests
{
    private readonly IRenewalRepository _renewalRepository = Substitute.For<IRenewalRepository>();
    private readonly IQuoteRepository _quoteRepository = Substitute.For<IQuoteRepository>();
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly AcceptRenewalCommandHandler _handler;

    public AcceptRenewalCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 7, 12, 0, 0, 0, DateTimeKind.Utc));
        // Staff kullanıcı → QuoteAuthorization.EnsureCanAccessAsync erken döner (müşteri araması gerekmez).
        _currentUserService.IsInRole(Roles.Admin).Returns(true);
        _handler = new AcceptRenewalCommandHandler(
            _renewalRepository, _quoteRepository, _customerRepository, _currentUserService,
            _dateTimeProvider, _unitOfWork, Substitute.For<ILogger<AcceptRenewalCommandHandler>>());
    }

    // Yeni dönem teklifi + yenileme kaydını (NewQuote navigasyonu yüklü) hazırlar. NewQuote private-set
    // olduğundan test için reflection ile bağlanır (EF navigation'ının test karşılığı).
    private Renewal BuildRenewalWithQuote(QuoteStatus targetStatus)
    {
        var quote = new Quote(Guid.NewGuid(), Guid.NewGuid(), InsuranceBranch.Kasko, Guid.NewGuid(), null);
        quote.MarkAsPriced(4200m, _dateTimeProvider.UtcNow.AddDays(20));
        if (targetStatus is QuoteStatus.Approved or QuoteStatus.Purchased)
        {
            quote.Approve();
        }
        if (targetStatus == QuoteStatus.Purchased)
        {
            quote.Purchase();
        }

        var renewal = new Renewal(Guid.NewGuid(), quote.Id, _dateTimeProvider.UtcNow);
        typeof(Renewal).GetProperty(nameof(Renewal.NewQuote))!.SetValue(renewal, quote);
        _renewalRepository.GetTrackedByIdAsync(renewal.Id, Arg.Any<CancellationToken>()).Returns(renewal);
        return renewal;
    }

    [Fact]
    public async Task Handle_Should_ApproveQuoteAndAcceptRenewal_When_QuotePriced()
    {
        var renewal = BuildRenewalWithQuote(QuoteStatus.Priced);

        await _handler.Handle(new AcceptRenewalCommand(renewal.Id), CancellationToken.None);

        renewal.NewQuote!.Status.Should().Be(QuoteStatus.Approved, "Priced teklif onaylanır → ödeme aşamasına hazır");
        renewal.IsAccepted.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact] // İdempotent: teklif başka yoldan (teklif detay ekranı) zaten onaylanmışsa hata vermez.
    public async Task Handle_Should_BeIdempotent_When_QuoteAlreadyApproved()
    {
        var renewal = BuildRenewalWithQuote(QuoteStatus.Approved);

        var act = () => _handler.Handle(new AcceptRenewalCommand(renewal.Id), CancellationToken.None);

        await act.Should().NotThrowAsync("zaten onaylı teklif tekrar Approve edilmez (idempotent)");
        renewal.NewQuote!.Status.Should().Be(QuoteStatus.Approved);
        renewal.IsAccepted.Should().BeTrue("yenileme kaydı tutarlı biçimde onaylı işaretlenir");
    }

    [Fact]
    public async Task Handle_Should_ThrowBusinessRule_When_QuoteAlreadyPurchased()
    {
        var renewal = BuildRenewalWithQuote(QuoteStatus.Purchased);

        var act = () => _handler.Handle(new AcceptRenewalCommand(renewal.Id), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_RenewalDoesNotExist()
    {
        _renewalRepository.GetTrackedByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Renewal?)null);

        var act = () => _handler.Handle(new AcceptRenewalCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_RenewalHasNoLinkedQuote()
    {
        // NewQuote navigasyonu yüklenmemiş/eksik renewal → yeni teklif bulunamadı.
        var renewal = new Renewal(Guid.NewGuid(), Guid.NewGuid(), _dateTimeProvider.UtcNow);
        _renewalRepository.GetTrackedByIdAsync(renewal.Id, Arg.Any<CancellationToken>()).Returns(renewal);

        var act = () => _handler.Handle(new AcceptRenewalCommand(renewal.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
