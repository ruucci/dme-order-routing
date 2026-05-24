namespace Dme.OrderRouting.Api.Models;

public class RouteResponse
{
    public bool Feasible { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<SupplierRoute> Routing { get; set; } = [];
}