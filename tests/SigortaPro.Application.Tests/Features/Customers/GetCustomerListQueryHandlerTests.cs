using FluentAssertions;
using NSubstitute;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Customers.Queries.GetCustomerList;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Tests.Features.Customers;

public class GetCustomerListQueryHandlerTests
{
    private readonly ICustomerRepository _customerRepository = Substitute.For<ICustomerRepository>();
    private readonly GetCustomerListQueryHandler _handler;

    public GetCustomerListQueryHandlerTests()
    {
        _handler = new GetCustomerListQueryHandler(_customerRepository);
    }

    [Fact]
    public async Task Handle_Should_MapEntitiesToMaskedSummaries_When_CustomersExist()
    {
        var customer = CustomerTestData.CreateCustomer(Guid.NewGuid(), Guid.NewGuid());
        var page = new PagedResult<Customer>(new[] { customer }, 1, 20, 1);

        _customerRepository.SearchAsync(
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<PaginationParams>(), Arg.Any<CancellationToken>())
            .Returns(page);

        var result = await _handler.Handle(new GetCustomerListQuery(SearchTerm: "Yılmaz"), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].LastName.Should().Be("Yılmaz");
        result.Items[0].City.Should().Be("İstanbul");
        // Listedeki TCKN de maskeli döner.
        result.Items[0].MaskedTckn.Should().Be("*********10");
    }

    [Fact]
    public async Task Handle_Should_PassFilterAndPagingToRepository()
    {
        _customerRepository.SearchAsync(
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<PaginationParams>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Customer>(Array.Empty<Customer>(), 2, 50, 0));

        await _handler.Handle(new GetCustomerListQuery(Page: 2, PageSize: 50, SearchTerm: "Ali", City: "Ankara"), CancellationToken.None);

        await _customerRepository.Received(1).SearchAsync(
            "Ali",
            "Ankara",
            Arg.Is<PaginationParams>(paging => paging.Page == 2 && paging.PageSize == 50),
            Arg.Any<CancellationToken>());
    }
}
