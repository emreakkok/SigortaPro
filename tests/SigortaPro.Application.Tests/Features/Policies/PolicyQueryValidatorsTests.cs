using FluentAssertions;
using SigortaPro.Application.Features.Policies.Queries.GetMyPolicies;
using SigortaPro.Application.Features.Policies.Queries.GetPolicyById;

namespace SigortaPro.Application.Tests.Features.Policies;

public class PolicyQueryValidatorsTests
{
    private readonly GetMyPoliciesQueryValidator _listValidator = new();
    private readonly GetPolicyByIdQueryValidator _detailValidator = new();

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    public void GetMyPolicies_Should_Fail_When_PagingOutOfRange(int page, int pageSize)
    {
        var result = _listValidator.Validate(new GetMyPoliciesQuery(page, pageSize));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetMyPolicies_Should_Pass_When_PagingValid()
    {
        var result = _listValidator.Validate(new GetMyPoliciesQuery(1, 20));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetPolicyById_Should_Fail_When_PolicyIdEmpty()
    {
        var result = _detailValidator.Validate(new GetPolicyByIdQuery(Guid.Empty));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetPolicyById_Should_Pass_When_PolicyIdProvided()
    {
        var result = _detailValidator.Validate(new GetPolicyByIdQuery(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }
}
