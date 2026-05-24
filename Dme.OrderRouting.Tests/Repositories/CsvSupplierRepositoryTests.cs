using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Dme.OrderRouting.Api.Configuration;
using Dme.OrderRouting.Api.Repositories;
using Dme.OrderRouting.Tests.TestSupport;

namespace Dme.OrderRouting.Tests.Repositories;

public class CsvSupplierRepositoryTests
{
    [Fact]
    public async Task GetSuppliersAsync_ShouldLoadSuppliers()
    {
        var repository = CreateRepository();

        var suppliers = await repository.GetSuppliersAsync();

        suppliers.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetSuppliersAsync_ShouldLoadSupplierNames()
    {
        var repository = CreateRepository();

        var suppliers = await repository.GetSuppliersAsync();

        suppliers.Should().Contain(supplier => !string.IsNullOrWhiteSpace(supplier.SupplierName));
    }

    [Fact]
    public async Task GetSuppliersAsync_ShouldParseProductCategories()
    {
        var repository = CreateRepository();

        var suppliers = await repository.GetSuppliersAsync();

        suppliers.Should().Contain(supplier => supplier.ProductCategories.Count > 0);
    }

    [Fact]
    public async Task GetSuppliersAsync_ShouldParseMailOrderFlag()
    {
        var repository = CreateRepository();

        var suppliers = await repository.GetSuppliersAsync();

        suppliers.Should().Contain(supplier => supplier.CanMailOrder);
    }

    [Fact]
    public async Task GetSuppliersAsync_ShouldHandleMissingRatings()
    {
        var repository = CreateRepository();

        var suppliers = await repository.GetSuppliersAsync();

        suppliers.Should().Contain(supplier => supplier.CustomerSatisfactionScore == null);
    }

    [Fact]
    public async Task GetSuppliersAsync_ShouldReturnCachedSuppliersAfterFirstLoad()
    {
        var repository = CreateRepository();

        var firstResult = await repository.GetSuppliersAsync();
        var secondResult = await repository.GetSuppliersAsync();

        secondResult.Should().BeSameAs(firstResult);
    }

    [Fact]
    public async Task GetSuppliersAsync_ShouldReturnEmptyList_WhenFileDoesNotExist()
    {
        var repository = CreateRepository("Data/missing-suppliers.csv");

        var suppliers = await repository.GetSuppliersAsync();

        suppliers.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSuppliersAsync_ShouldReturnSameInstance_WhenCalledMoreThanOnce()
    {
        var repository = CreateRepository();

        var firstResult = await repository.GetSuppliersAsync();
        var secondResult = await repository.GetSuppliersAsync();

        secondResult.Should().BeSameAs(firstResult);
    }

    private static CsvSupplierRepository CreateRepository(
        string suppliersPath = "Data/suppliers.csv")
    {
        var environment = new TestWebHostEnvironment
        {
            ContentRootPath = GetApiProjectPath()
        };

        var settings = Options.Create(new DataFileSettings
        {
            Suppliers = suppliersPath
        });

        return new CsvSupplierRepository(environment, NullLogger<CsvSupplierRepository>.Instance, settings);
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