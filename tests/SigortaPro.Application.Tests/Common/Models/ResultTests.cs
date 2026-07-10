using FluentAssertions;
using SigortaPro.Application.Common.Models;

namespace SigortaPro.Application.Tests.Common.Models;

public class ResultTests
{
    [Fact]
    public void Success_Should_ReturnSuccessResult_When_Called()
    {
        var result = Result<string>.Success("ok");

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be("ok");
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Failure_Should_ReturnFailureResult_When_ErrorsProvided()
    {
        var result = Result<string>.Failure("Hata oluştu.");

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().ContainSingle().Which.Should().Be("Hata oluştu.");
    }

    [Fact]
    public void Success_Should_ReturnSuccessResult_When_NonGenericResultUsed()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
