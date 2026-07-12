using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Policies.Commands.ExpireOverduePolicies;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Tests.Features.Renewals;

public class ExpireOverduePoliciesCommandHandlerTests
{
    private readonly IPolicyRepository _policyRepository = Substitute.For<IPolicyRepository>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ExpireOverduePoliciesCommandHandler _handler;

    private readonly DateTime _now = new(2026, 7, 12, 0, 0, 0, DateTimeKind.Utc);

    public ExpireOverduePoliciesCommandHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(_now);
        _handler = new ExpireOverduePoliciesCommandHandler(
            _policyRepository, _dateTimeProvider, _unitOfWork,
            Substitute.For<ILogger<ExpireOverduePoliciesCommandHandler>>());
    }

    private Policy OverdueActivePolicy() =>
        new("POL-2025-000001", Guid.NewGuid(), Guid.NewGuid(), _now.AddYears(-2), _now.AddYears(-1), 20000m);

    [Fact]
    public async Task Handle_Should_ExpirePoliciesAndReturnCount_When_OverduePoliciesExist()
    {
        var policy = OverdueActivePolicy();
        _policyRepository.GetOverdueActiveAsync(_now, Arg.Any<CancellationToken>()).Returns(new[] { policy });

        var count = await _handler.Handle(new ExpireOverduePoliciesCommand(), CancellationToken.None);

        count.Should().Be(1);
        policy.Status.Should().Be(PolicyStatus.Expired);
        _policyRepository.Received(1).Update(policy);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnZeroAndNotSave_When_NoOverduePolicies()
    {
        _policyRepository.GetOverdueActiveAsync(_now, Arg.Any<CancellationToken>()).Returns(Array.Empty<Policy>());

        var count = await _handler.Handle(new ExpireOverduePoliciesCommand(), CancellationToken.None);

        count.Should().Be(0);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
