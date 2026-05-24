namespace Dme.OrderRouting.Api.Models;

public class SupplierRoute
{
    public string SupplierId { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public List<RoutedItem> Items { get; set; } = [];
}