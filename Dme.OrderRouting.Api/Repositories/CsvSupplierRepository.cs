using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Microsoft.Extensions.Options;
using Dme.OrderRouting.Api.Configuration;
using Dme.OrderRouting.Api.Models;
using Dme.OrderRouting.Api.Repositories.Interfaces;
using System.Globalization;

namespace Dme.OrderRouting.Api.Repositories;

public class CsvSupplierRepository : ISupplierRepository
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<CsvSupplierRepository> _logger;
    private readonly DataFileSettings _settings;

    private IReadOnlyList<Supplier>? _suppliers;

    public CsvSupplierRepository(IWebHostEnvironment environment, ILogger<CsvSupplierRepository> logger, IOptions<DataFileSettings> options)
    {
        _environment = environment;
        _logger = logger;
        _settings = options.Value;
    }

    public async Task<IReadOnlyList<Supplier>> GetSuppliersAsync()
    {
        if (_suppliers is not null)
        {
            return _suppliers;
        }

        var path = Path.Combine(_environment.ContentRootPath, _settings.Suppliers);

        if (!File.Exists(path))
        {
            _logger.LogWarning("Suppliers CSV file was not found at path {Path}", path);
            return [];
        }

        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            TrimOptions = TrimOptions.Trim
        });

        var records = new List<Supplier>();

        await foreach (var row in csv.GetRecordsAsync<SupplierCsvRow>())
        {
            var supplier = new Supplier
            {
                SupplierId = row.SupplierId?.Trim() ?? string.Empty,
                SupplierName = row.SupplierName?.Trim() ?? string.Empty,
                ServiceZips = row.ServiceZips?.Trim() ?? string.Empty,
                ProductCategories = ParseCategories(row.ProductCategories),
                CustomerSatisfactionScore = ParseScore(row.CustomerSatisfactionScore),
                CanMailOrder = string.Equals(row.CanMailOrder?.Trim(), "y", StringComparison.OrdinalIgnoreCase)
            };

            if (string.IsNullOrWhiteSpace(supplier.SupplierId) ||
                string.IsNullOrWhiteSpace(supplier.SupplierName) ||
                supplier.ProductCategories.Count == 0)
            {
                _logger.LogWarning(
                    "Skipping malformed supplier record. SupplierId: {SupplierId}, SupplierName: {SupplierName}",
                    supplier.SupplierId,
                    supplier.SupplierName);

                continue;
            }

            records.Add(supplier);
        }

        _suppliers = records;

        return _suppliers;
    }

    private static List<string> ParseCategories(string? value)
    {
        return (value ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(category => category.ToLowerInvariant())
            .ToList();
    }

    private static decimal? ParseScore(string? value)
    {
        if (decimal.TryParse(value?.Trim(), out var score))
        {
            return score;
        }

        return null;
    }

    private sealed class SupplierCsvRow
    {
        [Name("supplier_id")]
        public string? SupplierId { get; set; }

        [Name("supplier_name")]
        public string? SupplierName { get; set; }

        [Name("service_zips")]
        public string? ServiceZips { get; set; }

        [Name("product_categories")]
        public string? ProductCategories { get; set; }

        [Name("customer_satisfaction_score")]
        public string? CustomerSatisfactionScore { get; set; }

        [Name("can_mail_order?")]
        public string? CanMailOrder { get; set; }
    }
}