using FluentAssertions;
using SigortaPro.Application.Features.Dashboard.Queries.GetPolicyReport;
using SigortaPro.Application.Features.Dashboard.Queries.GetRiskiestCustomers;

namespace SigortaPro.Application.Tests.Features.Dashboard;

public class DashboardReportValidatorTests
{
    private readonly GetPolicyReportQueryValidator _policyReportValidator = new();
    private readonly GetRiskiestCustomersQueryValidator _riskiestValidator = new();

    [Fact]
    public void PolicyReport_Should_Fail_When_ToBeforeFrom()
    {
        var query = new GetPolicyReportQuery(
            new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc));

        var result = _policyReportValidator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(GetPolicyReportQuery.To));
    }

    [Fact]
    public void PolicyReport_Should_Pass_When_RangeValid()
    {
        var query = new GetPolicyReportQuery(
            new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        _policyReportValidator.Validate(query).IsValid.Should().BeTrue();
    }

    [Fact]
    public void RiskiestCustomers_Should_Fail_When_TopOutOfRange()
    {
        _riskiestValidator.Validate(new GetRiskiestCustomersQuery(0)).IsValid.Should().BeFalse();
        _riskiestValidator.Validate(new GetRiskiestCustomersQuery(51)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RiskiestCustomers_Should_Pass_When_TopWithinRange()
    {
        _riskiestValidator.Validate(new GetRiskiestCustomersQuery(10)).IsValid.Should().BeTrue();
    }
}
