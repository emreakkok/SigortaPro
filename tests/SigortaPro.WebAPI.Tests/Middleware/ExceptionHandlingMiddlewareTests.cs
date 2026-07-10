using System.Text.Json;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SigortaPro.Application.Common.Exceptions;
using SigortaPro.WebAPI.Middleware;

namespace SigortaPro.WebAPI.Tests.Middleware;

public sealed class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_Should_Return400WithErrors_When_ValidationExceptionThrown()
    {
        // Arrange
        var failures = new[] { new ValidationFailure("TCKN", "TCKN 11 haneli olmalıdır.") };
        var exception = new ValidationException(failures);

        // Act
        var (statusCode, body) = await InvokeAsync(exception);

        // Assert
        statusCode.Should().Be(StatusCodes.Status400BadRequest);
        body.RootElement.GetProperty("status").GetInt32().Should().Be(400);
        body.RootElement.GetProperty("type").GetString().Should().EndWith("/validation");
        body.RootElement.GetProperty("errors").GetProperty("TCKN")[0].GetString()
            .Should().Be("TCKN 11 haneli olmalıdır.");
    }

    [Fact]
    public async Task InvokeAsync_Should_Return404_When_NotFoundExceptionThrown()
    {
        // Arrange
        var exception = new NotFoundException("Quote", Guid.NewGuid());

        // Act
        var (statusCode, body) = await InvokeAsync(exception);

        // Assert
        statusCode.Should().Be(StatusCodes.Status404NotFound);
        body.RootElement.GetProperty("status").GetInt32().Should().Be(404);
        body.RootElement.GetProperty("type").GetString().Should().EndWith("/not-found");
    }

    [Fact]
    public async Task InvokeAsync_Should_Return409_When_BusinessRuleExceptionThrown()
    {
        // Arrange
        var exception = new BusinessRuleException("Bu teklif zaten satın alınmış.");

        // Act
        var (statusCode, body) = await InvokeAsync(exception);

        // Assert
        statusCode.Should().Be(StatusCodes.Status409Conflict);
        body.RootElement.GetProperty("detail").GetString().Should().Be("Bu teklif zaten satın alınmış.");
    }

    [Fact]
    public async Task InvokeAsync_Should_Return403_When_ForbiddenAccessExceptionThrown()
    {
        // Arrange
        var exception = new ForbiddenAccessException();

        // Act
        var (statusCode, _) = await InvokeAsync(exception);

        // Assert
        statusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task InvokeAsync_Should_HideDetail_When_UnhandledExceptionInProduction()
    {
        // Arrange
        var exception = new InvalidOperationException("iç detay sızmamalı");

        // Act
        var (statusCode, body) = await InvokeAsync(exception, environment: Environments.Production);

        // Assert
        statusCode.Should().Be(StatusCodes.Status500InternalServerError);
        body.RootElement.GetProperty("detail").GetString().Should().NotContain("iç detay sızmamalı");
    }

    [Fact]
    public async Task InvokeAsync_Should_ExposeDetail_When_UnhandledExceptionInDevelopment()
    {
        // Arrange
        var exception = new InvalidOperationException("geliştirme detayı");

        // Act
        var (statusCode, body) = await InvokeAsync(exception, environment: Environments.Development);

        // Assert
        statusCode.Should().Be(StatusCodes.Status500InternalServerError);
        body.RootElement.GetProperty("detail").GetString().Should().Be("geliştirme detayı");
    }

    [Fact]
    public async Task InvokeAsync_Should_IncludeTraceId_When_ExceptionThrown()
    {
        // Arrange
        var exception = new NotFoundException("Policy", Guid.NewGuid());

        // Act
        var (_, body) = await InvokeAsync(exception);

        // Assert
        body.RootElement.TryGetProperty("traceId", out var traceId).Should().BeTrue();
        traceId.GetString().Should().NotBeNullOrEmpty();
    }

    private static async Task<(int StatusCode, JsonDocument Body)> InvokeAsync(
        Exception exception,
        string environment = "Production")
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider(),
        };
        context.Response.Body = new MemoryStream();

        var logger = Substitute.For<ILogger<ExceptionHandlingMiddleware>>();
        var hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.EnvironmentName = environment;

        var middleware = new ExceptionHandlingMiddleware(_ => throw exception, logger, hostEnvironment);

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await JsonDocument.ParseAsync(context.Response.Body);
        return (context.Response.StatusCode, body);
    }
}
