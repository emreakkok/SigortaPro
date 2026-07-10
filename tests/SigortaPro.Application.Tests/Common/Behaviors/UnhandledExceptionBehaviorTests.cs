using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SigortaPro.Application.Common.Behaviors;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.Domain.Common;

namespace SigortaPro.Application.Tests.Common.Behaviors;

public class UnhandledExceptionBehaviorTests
{
    public sealed record SampleRequest : IRequest<string>;

    [Fact]
    public async Task Handle_Should_ThrowBusinessRuleException_When_DomainExceptionThrown()
    {
        var logger = Substitute.For<ILogger<UnhandledExceptionBehavior<SampleRequest, string>>>();
        var behavior = new UnhandledExceptionBehavior<SampleRequest, string>(logger);

        Func<Task> act = () => behavior.Handle(
            new SampleRequest(),
            () => throw new DomainException("Geçersiz durum geçişi."),
            CancellationToken.None);

        (await act.Should().ThrowAsync<BusinessRuleException>())
            .WithMessage("Geçersiz durum geçişi.");
    }

    [Fact]
    public async Task Handle_Should_Rethrow_When_KnownApplicationExceptionThrown()
    {
        var logger = Substitute.For<ILogger<UnhandledExceptionBehavior<SampleRequest, string>>>();
        var behavior = new UnhandledExceptionBehavior<SampleRequest, string>(logger);

        Func<Task> act = () => behavior.Handle(
            new SampleRequest(),
            () => throw new NotFoundException("Quote", Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_ReturnResponse_When_NoExceptionThrown()
    {
        var logger = Substitute.For<ILogger<UnhandledExceptionBehavior<SampleRequest, string>>>();
        var behavior = new UnhandledExceptionBehavior<SampleRequest, string>(logger);

        var result = await behavior.Handle(new SampleRequest(), () => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
    }
}
