namespace Dme.OrderRouting.Api.Models;

public class Supplier
{
    public string SupplierId { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string ServiceZips { get; set; } = string.Empty;
    public List<string> ProductCategories { get; set; } = [];
    public decimal? CustomerSatisfactionScore { get; set; }
    public bool CanMailOrder { get; set; }
}