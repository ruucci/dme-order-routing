using Dme.OrderRouting.Api.Models;
using Dme.OrderRouting.Api.Repositories.Interfaces;
using Dme.OrderRouting.Api.Services.Interfaces;

namespace Dme.OrderRouting.Api.Services;

public class OrderRoutingService : IOrderRoutingService
{
    private readonly IProductRepository _productRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IZipCoverageService _zipCoverageService;
    private readonly ILogger<OrderRoutingService> _logger;
    private const decimal SimilarRatingThreshold = 1.0m;
    private const string LocalFulfillmentMode = "local";
    private const string MailOrderFulfillmentMode = "mail_order";

    public OrderRoutingService(
        IProductRepository productRepository,
        ISupplierRepository supplierRepository,
        IZipCoverageService zipCoverageService,
        ILogger<OrderRoutingService> logger)
    {
        _productRepository = productRepository;
        _supplierRepository = supplierRepository;
        _zipCoverageService = zipCoverageService;
        _logger = logger;
    }

    public async Task<RouteResponse> RouteAsync(OrderRequest request)
    {
        var validationErrors = ValidateRequest(request);

        if (validationErrors.Count > 0)
        {
            return new RouteResponse
            {
                Feasible = false,
                Errors = validationErrors
            };
        }

        var items = request.Items ?? [];

        _logger.LogInformation("Routing order {OrderId} with {ItemCount} items", request.OrderId, items.Count);

        var productsByCode = await _productRepository.GetProductsByCodeAsync();

        if (productsByCode.Count == 0)
        {
            _logger.LogError("No products were loaded from the configured product data source.");

            return new RouteResponse
            {
                Feasible = false,
                Errors = ["Product reference data is unavailable."]
            };
        }

        var suppliers = await _supplierRepository.GetSuppliersAsync();

        if (suppliers.Count == 0)
        {
            _logger.LogError("No suppliers were loaded from the configured supplier data source.");

            return new RouteResponse
            {
                Feasible = false,
                Errors = ["Supplier reference data is unavailable."]
            };
        }

        var resolvedItems = ResolveItems(request, productsByCode);

        if (resolvedItems.Errors.Count > 0)
        {
            return new RouteResponse
            {
                Feasible = false,
                Errors = resolvedItems.Errors
            };
        }

        var singleSupplierRoute = TryRouteToSingleSupplier(request, resolvedItems.Items, suppliers);

        if (singleSupplierRoute is not null)
        {
            return new RouteResponse
            {
                Feasible = true,
                Routing = [singleSupplierRoute]
            };
        }

        return RouteWithSplitShipments(request, resolvedItems.Items, suppliers);
    }

    private static List<string> ValidateRequest(OrderRequest request)
    {
        var errors = new List<string>();

        ValidateCustomerZip(request.CustomerZip, errors);
        ValidateItems(request.Items, errors);

        return errors;
    }

    private static void ValidateCustomerZip(string? customerZip, List<string> errors)
    {
        var normalizedZip = customerZip?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedZip) || normalizedZip.Length != 5 || !normalizedZip.All(char.IsDigit))
        {
            errors.Add("Order must include a valid customer_zip.");
        }
    }

    private static void ValidateItems(IReadOnlyList<OrderItem>? items, List<string> errors)
    {
        if (items is null || items.Count == 0)
        {
            errors.Add("Order must include at least one line item.");
            return;
        }

        foreach (var item in items)
        {
            ValidateItem(item, errors);
        }
    }

    private static void ValidateItem(OrderItem item, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(item.ProductCode))
        {
            errors.Add("Each line item must include a valid product_code.");
        }

        if (item.Quantity <= 0)
        {
            errors.Add($"Quantity must be greater than zero for product_code '{item.ProductCode}'.");
        }
    }

    private static ResolvedItemsResult ResolveItems(OrderRequest request, IReadOnlyDictionary<string, Product> productsByCode)
    {
        var errors = new List<string>();
        var items = new List<ResolvedOrderItem>();

        var requestItems = request.Items ?? [];

        foreach (var item in requestItems)
        {
            if (!productsByCode.TryGetValue(item.ProductCode ?? string.Empty, out var product))
            {
                errors.Add($"Unknown product_code '{item.ProductCode}'.");
                continue;
            }

            items.Add(new ResolvedOrderItem(item.ProductCode!, item.Quantity, product.Category));
        }

        var mergedItems = items
            .GroupBy(item => item.ProductCode, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ResolvedOrderItem(group.First().ProductCode, group.Sum(item => item.Quantity), group.First().Category))
            .ToList();

        return new ResolvedItemsResult(mergedItems, errors);
    }

    private SupplierRoute? TryRouteToSingleSupplier(
    OrderRequest request,
    IReadOnlyList<ResolvedOrderItem> items,
    IReadOnlyList<Supplier> suppliers)
    {
        var candidates = suppliers
            .Select(supplier => new SupplierCandidate(supplier, GetFulfillmentMode(supplier, request)))
            .Where(candidate => candidate.FulfillmentMode is not null)
            .Where(candidate => items.All(item => SupplierCanFulfillCategory(candidate.Supplier, item.Category)))
            .ToList();

        candidates.Sort(CompareSupplierCandidates);

        var bestCandidate = candidates.FirstOrDefault();

        if (bestCandidate is null)
        {
            return null;
        }

        return BuildSupplierRoute(bestCandidate.Supplier, bestCandidate.FulfillmentMode!, items);
    }

    private RouteResponse RouteWithSplitShipments(
        OrderRequest request,
        IReadOnlyList<ResolvedOrderItem> items,
        IReadOnlyList<Supplier> suppliers)
    {
        var routesBySupplierId = new Dictionary<string, SupplierRoute>();
        var errors = new List<string>();

        foreach (var item in items)
        {
            var candidates = suppliers
                .Select(supplier => new SupplierCandidate(supplier, GetFulfillmentMode(supplier, request)))
                .Where(candidate => candidate.FulfillmentMode is not null)
                .Where(candidate => SupplierCanFulfillCategory(candidate.Supplier, item.Category))
                .ToList();

            candidates.Sort(CompareSupplierCandidates);

            var bestSupplier = candidates.FirstOrDefault();

            if (bestSupplier is null)
            {
                errors.Add($"No eligible supplier found for product_code '{item.ProductCode}' in category '{item.Category}'.");
                continue;
            }

            if (!routesBySupplierId.TryGetValue(bestSupplier.Supplier.SupplierId, out var route))
            {
                route = new SupplierRoute
                {
                    SupplierId = bestSupplier.Supplier.SupplierId,
                    SupplierName = bestSupplier.Supplier.SupplierName
                };

                routesBySupplierId[bestSupplier.Supplier.SupplierId] = route;
            }

            route.Items.Add(new RoutedItem
            {
                ProductCode = item.ProductCode,
                Quantity = item.Quantity,
                Category = item.Category,
                FulfillmentMode = bestSupplier.FulfillmentMode!
            });
        }

        if (errors.Count > 0)
        {
            return new RouteResponse
            {
                Feasible = false,
                Errors = errors
            };
        }

        return new RouteResponse
        {
            Feasible = true,
            Routing = routesBySupplierId.Values.ToList()
        };
    }

    private string? GetFulfillmentMode(Supplier supplier, OrderRequest request)
    {
        var servesCustomerZip = _zipCoverageService.ServesZip(supplier.ServiceZips, request.CustomerZip ?? string.Empty);

        if (servesCustomerZip)
        {
            return LocalFulfillmentMode;
        }

        if (request.MailOrder && supplier.CanMailOrder)
        {
            return MailOrderFulfillmentMode;
        }

        return null;
    }

    private static SupplierRoute BuildSupplierRoute(Supplier supplier, string fulfillmentMode, IReadOnlyList<ResolvedOrderItem> items)
    {
        return new SupplierRoute
        {
            SupplierId = supplier.SupplierId,
            SupplierName = supplier.SupplierName,
            Items = items.Select(item => new RoutedItem
            {
                ProductCode = item.ProductCode,
                Quantity = item.Quantity,
                Category = item.Category,
                FulfillmentMode = fulfillmentMode
            }).ToList()
        };
    }

    private static bool SupplierCanFulfillCategory(Supplier supplier, string productCategory)
    {
        return supplier.ProductCategories.Any(supplierCategory =>
            string.Equals(supplierCategory, productCategory, StringComparison.OrdinalIgnoreCase) ||
            productCategory.Contains(supplierCategory, StringComparison.OrdinalIgnoreCase) ||
            supplierCategory.Contains(productCategory, StringComparison.OrdinalIgnoreCase));
    }

    private static int CompareSupplierCandidates(SupplierCandidate first, SupplierCandidate second)
    {
        var firstScore = first.Supplier.CustomerSatisfactionScore ?? 0;
        var secondScore = second.Supplier.CustomerSatisfactionScore ?? 0;

        var scoresAreSimilar = Math.Abs(firstScore - secondScore) <= SimilarRatingThreshold;

        if (scoresAreSimilar)
        {
            var localComparison = IsLocal(second).CompareTo(IsLocal(first));

            if (localComparison != 0)
            {
                return localComparison;
            }
        }

        return secondScore.CompareTo(firstScore);
    }

    private static bool IsLocal(SupplierCandidate candidate)
    {
        return candidate.FulfillmentMode == "local";
    }

    private sealed record SupplierCandidate(Supplier Supplier, string? FulfillmentMode);
    private sealed record ResolvedOrderItem(string ProductCode, int Quantity, string Category);
    private sealed record ResolvedItemsResult(IReadOnlyList<ResolvedOrderItem> Items, List<string> Errors);
}