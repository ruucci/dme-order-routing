using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Dme.OrderRouting.Api.Configuration;
using Dme.OrderRouting.Api.Repositories;
using Dme.OrderRouting.Tests.TestSupport;

namespace Dme.OrderRouting.Tests.Repositories;

public class CsvProductRepositoryTests
{
    [Fact]
    public async Task GetProductsByCodeAsync_ShouldLoadProducts()
    {
        var repository = CreateRepository();

        var products = await repository.GetProductsByCodeAsync();

        products.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetProductsByCodeAsync_ShouldLoadKnownProduct()
    {
        var repository = CreateRepository();

        var products = await repository.GetProductsByCodeAsync();

        products.Should().ContainKey("WC-STD-001");

        products["WC-STD-001"].Category.Should().Be("wheelchair");
    }

    [Fact]
    public async Task GetProductsByCodeAsync_ShouldReturnCachedProductsAfterFirstLoad()
    {
        var repository = CreateRepository();

        var firstResult = await repository.GetProductsByCodeAsync();
        var secondResult = await repository.GetProductsByCodeAsync();

        secondResult.Should().BeSameAs(firstResult);
    }

    [Fact]
    public async Task GetProductsByCodeAsync_ShouldReturnEmptyDictionary_WhenFileDoesNotExist()
    {
        var repository = CreateRepository("Data/missing-products.csv");

        var products = await repository.GetProductsByCodeAsync();

        products.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProductsByCodeAsync_ShouldReturnSameInstance_WhenCalledMoreThanOnce()
    {
        var repository = CreateRepository();

        var firstResult = await repository.GetProductsByCodeAsync();
        var secondResult = await repository.GetProductsByCodeAsync();

        secondResult.Should().BeSameAs(firstResult);
    }

    private static CsvProductRepository CreateRepository(
        string productsPath = "Data/products.csv")
    {
        var environment = new TestWebHostEnvironment
        {
            ContentRootPath = GetApiProjectPath()
        };

        var settings = Options.Create(new DataFileSettings
        {
            Products = productsPath
        });

        return new CsvProductRepository(environment, NullLogger<CsvProductRepository>.Instance, settings);
    }

    private static string GetApiProjectPath()
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Dme.OrderRouting.Api"));
    }
}