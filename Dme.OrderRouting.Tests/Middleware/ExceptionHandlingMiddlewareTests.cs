using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Dme.OrderRouting.Api.Middleware;
using Dme.OrderRouting.Api.Models;

namespace Dme.OrderRouting.Tests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldReturnHttp200_WhenUnhandledExceptionOccurs()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("Test exception"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturnInfeasibleResponse_WhenUnhandledExceptionOccurs()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("Test exception"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();

        var response = JsonSerializer.Deserialize<RouteResponse>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        response.Should().NotBeNull();
        response!.Feasible.Should().BeFalse();
        response.Errors.Should().Contain("Unexpected server error.");
    }
}