using FluentAssertions;
using SigortaPro.Application.Common.Models;

namespace SigortaPro.Application.Tests.Common.Models;

public class PagedResultTests
{
    [Fact]
    public void TotalPages_Should_RoundUp_When_TotalCountNotDivisibleByPageSize()
    {
        var pagedResult = new PagedResult<string>(new List<string> { "a", "b" }, page: 1, pageSize: 10, totalCount: 25);

        pagedResult.TotalPages.Should().Be(3);
    }

    [Fact]
    public void PageSize_Should_FallBackToDefault_When_ValueIsZeroOrNegative()
    {
        var pagination = new PaginationParams { PageSize = 0 };

        pagination.PageSize.Should().Be(20);
    }

    [Fact]
    public void PageSize_Should_CapAtMax_When_ValueExceedsMax()
    {
        var pagination = new PaginationParams { PageSize = 500 };

        pagination.PageSize.Should().Be(100);
    }

    [Fact]
    public void PageSize_Should_KeepValue_When_WithinAllowedRange()
    {
        var pagination = new PaginationParams { PageSize = 50 };

        pagination.PageSize.Should().Be(50);
    }
}
