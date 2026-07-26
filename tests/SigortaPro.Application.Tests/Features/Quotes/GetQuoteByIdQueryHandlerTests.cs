using FluentAssertions;
using NSubstitute;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Quotes.Queries.GetQuoteById;
using SigortaPro.Application.Tests.Features.Customers;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;
using SigortaPro.Application.Tests.Common;

namespace SigortaPro.Application.Tests.Features.Quotes;

public class GetQuoteByIdQueryHandlerTests
{
    private readonly IQuoteRepository _quoteRepository = Substitute.For<IQuoteRepository>();
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly IPricingEngine _pricingEngine = Substitute.For<IPricingEngine>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetQuoteByIdQueryHandler _handler;

    public GetQuoteByIdQueryHandlerTests()
    {
        _handler = new GetQuoteByIdQueryHandler(
            _quoteRepository, _customerRepository, _pricingEngine, PricingTestDoubles.BaselineResolver(), _currentUserService);
    }

    [Fact]
    public async Task Handle_Should_ThrowNotFound_When_QuoteDoesNotExist()
    {
        _quoteRepository.GetDetailByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Quote?)null);

        var act = () => _handler.Handle(new GetQuoteByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ThrowForbidden_When_QuoteBelongsToAnotherCustomer()
    {
        var quoteOwnerId = Guid.NewGuid();
        var quote = new Quote(quoteOwnerId, Guid.NewGuid(), InsuranceBranch.Kasko, Guid.NewGuid(), null);
        _quoteRepository.GetDetailByIdAsync(quote.Id, Arg.Any<CancellationToken>()).Returns(quote);

        // Çağıran, teklifin sahibi olmayan bir müşteri.
        var callerAppUserId = Guid.NewGuid();
        var caller = CustomerTestData.CreateCustomer(callerAppUserId, Guid.NewGuid());
        _currentUserService.IsInRole(Arg.Any<string>()).Returns(false);
        _currentUserService.UserId.Returns(callerAppUserId);
        _customerRepository.GetTrackedByAppUserIdAsync(callerAppUserId, Arg.Any<CancellationToken>()).Returns(caller);

        var act = () => _handler.Handle(new GetQuoteByIdQuery(quote.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        // Sahiplik reddi fiyatlama yeniden hesaplamasından önce gerçekleşir.
        _pricingEngine.DidNotReceive().CalculatePremium(Arg.Any<Application.Common.Pricing.PricingRequest>());
    }
}
