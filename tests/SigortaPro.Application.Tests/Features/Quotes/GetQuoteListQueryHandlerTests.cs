using FluentAssertions;
using NSubstitute;
using SigortaPro.Application.Common.Authorization;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Quotes.Queries.GetQuoteList;
using SigortaPro.Application.Tests.Features.Customers;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Tests.Features.Quotes;

public class GetQuoteListQueryHandlerTests
{
    private readonly IQuoteRepository _quoteRepository = Substitute.For<IQuoteRepository>();
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetQuoteListQueryHandler _handler;

    public GetQuoteListQueryHandlerTests()
    {
        _quoteRepository.SearchAsync(
                Arg.Any<Guid?>(), Arg.Any<QuoteStatus?>(), Arg.Any<InsuranceBranch?>(),
                Arg.Any<string?>(), Arg.Any<PaginationParams>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Quote>(Array.Empty<Quote>(), 1, 20, 0));

        _handler = new GetQuoteListQueryHandler(_quoteRepository, _customerRepository, _currentUserService);
    }

    [Fact]
    public async Task Handle_Should_ListAllQuotes_When_CallerIsStaff()
    {
        _currentUserService.IsInRole(Roles.Admin).Returns(true);

        await _handler.Handle(new GetQuoteListQuery(), CancellationToken.None);

        // Personel tüm teklifleri görür → müşteri filtresi yok (null).
        await _quoteRepository.Received(1).SearchAsync(
            null, Arg.Any<QuoteStatus?>(), Arg.Any<InsuranceBranch?>(),
            Arg.Any<string?>(), Arg.Any<PaginationParams>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_FilterToOwnQuotes_When_CallerIsCustomer()
    {
        var appUserId = Guid.NewGuid();
        var customer = CustomerTestData.CreateCustomer(appUserId, Guid.NewGuid());
        _currentUserService.IsInRole(Arg.Any<string>()).Returns(false);
        _currentUserService.UserId.Returns(appUserId);
        _customerRepository.GetTrackedByAppUserIdAsync(appUserId, Arg.Any<CancellationToken>()).Returns(customer);

        await _handler.Handle(new GetQuoteListQuery(), CancellationToken.None);

        await _quoteRepository.Received(1).SearchAsync(
            customer.Id, Arg.Any<QuoteStatus?>(), Arg.Any<InsuranceBranch?>(),
            Arg.Any<string?>(), Arg.Any<PaginationParams>(), Arg.Any<CancellationToken>());
    }
}
