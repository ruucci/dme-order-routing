using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Dme.OrderRouting.Api.Controllers;
using Dme.OrderRouting.Api.Models;
using Dme.OrderRouting.Api.Services;
using Dme.OrderRouting.Api.Services.Interfaces;

namespace Dme.OrderRouting.Tests.Controllers;

public class RouteControllerTests
{
    [Fact]
    public async Task Route_ShouldReturnOkResponse()
    {
        var expectedResponse = new RouteResponse
        {
            Feasible = true
        };

        var controller = new RouteController(new StubOrderRoutingService(expectedResponse));

        var request = new OrderRequest
        {
            OrderId = "ORD-TEST",
            CustomerZip = "10015",
            Items =
            [
                new OrderItem
                {
                    ProductCode = "WC-STD-001",
                    Quantity = 1
                }
            ]
        };

        var result = await controller.Route(request);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    private class StubOrderRoutingService : IOrderRoutingService
    {
        private readonly RouteResponse _response;

        public StubOrderRoutingService(RouteResponse response)
        {
            _response = response;
        }

        public Task<RouteResponse> RouteAsync(OrderRequest request)
        {
            return Task.FromResult(_response);
        }
    }
}