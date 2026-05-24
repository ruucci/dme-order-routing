using Microsoft.AspNetCore.Mvc;
using Dme.OrderRouting.Api.Models;
using Dme.OrderRouting.Api.Services.Interfaces;

namespace Dme.OrderRouting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RouteController : ControllerBase
{
    private readonly IOrderRoutingService _orderRoutingService;

    public RouteController(IOrderRoutingService orderRoutingService)
    {
        _orderRoutingService = orderRoutingService;
    }

    [HttpPost]
    public async Task<ActionResult<RouteResponse>> Route([FromBody] OrderRequest request)
    {
        var response = await _orderRoutingService.RouteAsync(request);
        return Ok(response);
    }
}