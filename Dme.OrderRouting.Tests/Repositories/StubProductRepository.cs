using Dme.OrderRouting.Api.Models;
using Dme.OrderRouting.Api.Repositories.Interfaces;

namespace Dme.OrderRouting.Tests.Repositories;

public class StubProductRepository : IProductRepository
{
    private readonly IReadOnlyDictionary<string, Product> _products;

    public StubProductRepository(IReadOnlyDictionary<string, Product> products)
    {
        _products = products;
    }

    public Task<IReadOnlyDictionary<string, Product>> GetProductsByCodeAsync()
    {
        return Task.FromResult(_products);
    }
}