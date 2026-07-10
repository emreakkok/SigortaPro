using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SigortaPro.Application.Common.Behaviors;

namespace SigortaPro.Application.Tests.Common.Behaviors;

public class PerformanceBehaviorTests
{
    public sealed record SampleRequest : IRequest<string>;

    [Fact]
    public async Task Handle_Should_ReturnResponse_When_NextSucceeds()
    {
        var logger = Substitute.For<ILogger<PerformanceBehavior<SampleRequest, string>>>();
        var behavior = new PerformanceBehavior<SampleRequest, string>(logger);

        var result = await behavior.Handle(new SampleRequest(), () => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
    }
}
