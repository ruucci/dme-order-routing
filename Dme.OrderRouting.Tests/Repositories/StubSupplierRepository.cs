using Dme.OrderRouting.Api.Models;
using Dme.OrderRouting.Api.Repositories.Interfaces;

namespace Dme.OrderRouting.Tests.Repositories;

public class StubSupplierRepository : ISupplierRepository
{
    private readonly IReadOnlyList<Supplier> _suppliers;

    public StubSupplierRepository(IReadOnlyList<Supplier> suppliers)
    {
        _suppliers = suppliers;
    }

    public Task<IReadOnlyList<Supplier>> GetSuppliersAsync()
    {
        return Task.FromResult(_suppliers);
    }
}