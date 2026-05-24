using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Microsoft.Extensions.Options;
using Dme.OrderRouting.Api.Configuration;
using Dme.OrderRouting.Api.Models;
using Dme.OrderRouting.Api.Repositories.Interfaces;
using System.Globalization;

namespace Dme.OrderRouting.Api.Repositories;

public class CsvProductRepository : IProductRepository
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<CsvProductRepository> _logger;
    private readonly DataFileSettings _settings;

    private IReadOnlyDictionary<string, Product>? _productsByCode;

    public CsvProductRepository(IWebHostEnvironment environment, ILogger<CsvProductRepository> logger, IOptions<DataFileSettings> options)
    {
        _environment = environment;
        _logger = logger;
        _settings = options.Value;
    }

    public async Task<IReadOnlyDictionary<string, Product>> GetProductsByCodeAsync()
    {
        if (_productsByCode is not null)
        {
            return _productsByCode;
        }

        var path = Path.Combine(_environment.ContentRootPath, _settings.Products);

        if (!File.Exists(path))
        {
            _logger.LogWarning("Products CSV file was not found at path {Path}", path);

            return new Dictionary<string, Product>();
        }

        using var reader = new StreamReader(path);

        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            TrimOptions = TrimOptions.Trim
        });

        var productsByCode = new Dictionary<string, Product>(StringComparer.OrdinalIgnoreCase);

            await foreach (var row in csv.GetRecordsAsync<ProductCsvRow>())
            {
                var product = ParseProduct(row);

                if (string.IsNullOrWhiteSpace(product.ProductCode) || string.IsNullOrWhiteSpace(product.Category))
                {
                    _logger.LogWarning(
                        "Skipping malformed product record. ProductCode: {ProductCode}, Category: {Category}",
                        product.ProductCode,
                        product.Category);

                    continue;
                }

                productsByCode.TryAdd(product.ProductCode, product);
            }

            _productsByCode = productsByCode;

            return _productsByCode;
    }

    private static Product ParseProduct(ProductCsvRow row)
    {
        return new Product
        {
            ProductCode = row.ProductCode?.Trim() ?? string.Empty,
            Category = row.Category?.Trim().ToLowerInvariant() ?? string.Empty
        };
    }

    private sealed class ProductCsvRow
    {
        [Name("product_code")]
        public string? ProductCode { get; set; }

        [Name("category")]
        public string? Category { get; set; }
    }
}