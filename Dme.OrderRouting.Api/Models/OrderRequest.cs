namespace Dme.OrderRouting.Api.Models;

public class OrderRequest
{
    public string? OrderId { get; set; }
    public string? CustomerZip { get; set; }
    public bool MailOrder { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}