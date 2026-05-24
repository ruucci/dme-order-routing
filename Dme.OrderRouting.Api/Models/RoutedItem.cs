namespace Dme.OrderRouting.Api.Models;

public class RoutedItem
{
    public string ProductCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Category { get; set; } = string.Empty;
    public string FulfillmentMode { get; set; } = string.Empty;
}