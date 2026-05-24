using Dme.OrderRouting.Api.Models;

namespace Dme.OrderRouting.Api.Repositories.Interfaces;

public interface IProductRepository
{
    Task<IReadOnlyDictionary<string, Product>> GetProductsByCodeAsync();
}