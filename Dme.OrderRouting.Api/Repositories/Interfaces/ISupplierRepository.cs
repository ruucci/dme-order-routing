using Dme.OrderRouting.Api.Models;

namespace Dme.OrderRouting.Api.Repositories.Interfaces;

public interface ISupplierRepository
{
    Task<IReadOnlyList<Supplier>> GetSuppliersAsync();
}