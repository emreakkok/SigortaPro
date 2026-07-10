using FluentAssertions;
using Microsoft.AspNetCore.Http;
using SigortaPro.WebAPI.Extensions;
using SigortaPro.WebAPI.Middleware;

namespace SigortaPro.WebAPI.Tests.Middleware;

public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_Should_ReuseIncomingCorrelationId_When_HeaderPresent()
    {
        // Arrange
        const string incoming = "test-correlation-123";
        var context = new DefaultHttpContext();
        context.Request.Headers[WebApiConstants.CorrelationIdHeader] = incoming;
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Items[WebApiConstants.CorrelationIdItemKey].Should().Be(incoming);
    }

    [Fact]
    public async Task InvokeAsync_Should_GenerateCorrelationId_When_HeaderMissing()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Items[WebApiConstants.CorrelationIdItemKey].Should().NotBeNull();
        context.Items[WebApiConstants.CorrelationIdItemKey]!.ToString().Should().NotBeNullOrWhiteSpace();
    }
}
