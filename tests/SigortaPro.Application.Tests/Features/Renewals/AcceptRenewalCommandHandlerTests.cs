using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Renewals.Commands.AcceptRenewal;
using SigortaPro.Domain.Entities;

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
        _handler = new AcceptRenewalCommandHandler(
            _renewalRepository, _quoteRepository, _customerRepository, _currentUserService,
            _dateTimeProvider, _unitOfWork, Substitute.For<ILogger<AcceptRenewalCommandHandler>>());
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
