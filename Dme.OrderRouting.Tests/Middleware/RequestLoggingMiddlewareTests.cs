using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Dme.OrderRouting.Api.Middleware;

namespace Dme.OrderRouting.Tests.Middleware;

public class RequestLoggingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldCallNextMiddleware()
    {
        var wasCalled = false;

        var context = new DefaultHttpContext();

        var middleware = new RequestLoggingMiddleware(
            _ =>
            {
                wasCalled = true;
                return Task.CompletedTask;
            },
            NullLogger<RequestLoggingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        wasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ShouldPreserveResponseStatusCode()
    {
        var context = new DefaultHttpContext();

        var middleware = new RequestLoggingMiddleware(
            httpContext =>
            {
                httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
                return Task.CompletedTask;
            },
            NullLogger<RequestLoggingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }
}