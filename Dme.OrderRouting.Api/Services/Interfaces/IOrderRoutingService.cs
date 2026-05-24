using Dme.OrderRouting.Api.Models;

namespace Dme.OrderRouting.Api.Services.Interfaces;

public interface IOrderRoutingService
{
    Task<RouteResponse> RouteAsync(OrderRequest request);
}