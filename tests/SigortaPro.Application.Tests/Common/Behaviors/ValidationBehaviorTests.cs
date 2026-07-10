using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NSubstitute;
using SigortaPro.Application.Common.Behaviors;
using ApplicationValidationException = SigortaPro.Application.Common.Exceptions.ValidationException;

namespace SigortaPro.Application.Tests.Common.Behaviors;

public class ValidationBehaviorTests
{
    public sealed record SampleRequest(string Name) : IRequest<string>;

    [Fact]
    public async Task Handle_Should_CallNext_When_NoValidatorsRegistered()
    {
        var behavior = new ValidationBehavior<SampleRequest, string>(Array.Empty<IValidator<SampleRequest>>());
        var request = new SampleRequest("test");

        var result = await behavior.Handle(request, () => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_Should_ThrowValidationException_When_ValidatorFails()
    {
        var validator = Substitute.For<IValidator<SampleRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<SampleRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { new ValidationFailure("Name", "Name boş olamaz.") }));

        var behavior = new ValidationBehavior<SampleRequest, string>(new[] { validator });
        var request = new SampleRequest(string.Empty);

        Func<Task> act = () => behavior.Handle(request, () => Task.FromResult("ok"), CancellationToken.None);

        await act.Should().ThrowAsync<ApplicationValidationException>();
    }

    [Fact]
    public async Task Handle_Should_CallNext_When_ValidatorPasses()
    {
        var validator = Substitute.For<IValidator<SampleRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<SampleRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var behavior = new ValidationBehavior<SampleRequest, string>(new[] { validator });
        var request = new SampleRequest("test");

        var result = await behavior.Handle(request, () => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
    }
}
