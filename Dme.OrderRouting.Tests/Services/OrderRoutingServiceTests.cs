using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Dme.OrderRouting.Api.Models;
using Dme.OrderRouting.Api.Services;
using Dme.OrderRouting.Tests.Repositories;

namespace Dme.OrderRouting.Tests.Services;

public class OrderRoutingServiceTests
{
    [Fact]
    public async Task RouteAsync_ShouldReturnValidationError_WhenOrderHasNoItems()
    {
        var service = CreateService();

        var request = new OrderRequest
        {
            OrderId = "ORD-TEST",
            CustomerZip = "10015",
            Items = []
        };

        var result = await service.RouteAsync(request);

        result.Feasible.Should().BeFalse();
        result.Errors.Should().Contain("Order must include at least one line item.");
    }

    [Fact]
    public async Task RouteAsync_ShouldReturnValidationError_WhenZipIsInvalid()
    {
        var service = CreateService();

        var request = new OrderRequest
        {
            OrderId = "ORD-TEST",
            CustomerZip = "ABC",
            Items =
            [
                new OrderItem
                {
                    ProductCode = "WC-STD-001",
                    Quantity = 1
                }
            ]
        };

        var result = await service.RouteAsync(request);

        result.Feasible.Should().BeFalse();
        result.Errors.Should().Contain("Order must include a valid customer_zip.");
    }

    [Fact]
    public async Task RouteAsync_ShouldReturnValidationError_WhenProductCodeIsMissing()
    {
        var service = CreateService();

        var request = new OrderRequest
        {
            OrderId = "ORD-TEST",
            CustomerZip = "10015",
            Items =
            [
                new OrderItem
            {
                ProductCode = "",
                Quantity = 1
            }
            ]
        };

        var result = await service.RouteAsync(request);

        result.Feasible.Should().BeFalse();
        result.Errors.Should().Contain("Each line item must include a valid product_code.");
    }

    [Fact]
    public async Task RouteAsync_ShouldReturnValidationError_WhenQuantityIsZero()
    {
        var service = CreateService();

        var request = new OrderRequest
        {
            OrderId = "ORD-TEST",
            CustomerZip = "10015",
            Items =
            [
                new OrderItem
            {
                ProductCode = "WC-STD-001",
                Quantity = 0
            }
            ]
        };

        var result = await service.RouteAsync(request);

        result.Feasible.Should().BeFalse();
        result.Errors.Should().Contain("Quantity must be greater than zero for product_code 'WC-STD-001'.");
    }

    [Fact]
    public async Task RouteAsync_ShouldReturnError_WhenProductCodeIsUnknown()
    {
        var service = CreateService();

        var request = new OrderRequest
        {
            OrderId = "ORD-TEST",
            CustomerZip = "10015",
            Items =
            [
                new OrderItem
                {
                    ProductCode = "UNKNOWN",
                    Quantity = 1
                }
            ]
        };

        var result = await service.RouteAsync(request);

        result.Feasible.Should().BeFalse();
        result.Errors.Should().Contain("Unknown product_code 'UNKNOWN'.");
    }

    [Fact]
    public async Task RouteAsync_ShouldPreferSingleSupplier_WhenOneCanFulfillEntireOrder()
    {
        var service = CreateService();

        var request = new OrderRequest
        {
            OrderId = "ORD-TEST",
            CustomerZip = "10015",
            MailOrder = false,
            Items =
            [
                new OrderItem
                {
                    ProductCode = "WC-STD-001",
                    Quantity = 1
                },
                new OrderItem
                {
                    ProductCode = "OX-PORT-024",
                    Quantity = 1
                }
            ]
        };

        var result = await service.RouteAsync(request);

        result.Feasible.Should().BeTrue();
        result.Routing.Should().HaveCount(1);
        result.Routing[0].SupplierId.Should().Be("SUP-001");
        result.Routing[0].Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task RouteAsync_ShouldSplitShipments_WhenNoSingleSupplierCanFulfillAllItems()
    {
        var service = CreateService(includeConsolidatedSupplier: false);

        var request = new OrderRequest
        {
            OrderId = "ORD-TEST",
            CustomerZip = "10015",
            MailOrder = false,
            Items =
            [
                new OrderItem
                {
                    ProductCode = "WC-STD-001",
                    Quantity = 1
                },
                new OrderItem
                {
                    ProductCode = "OX-PORT-024",
                    Quantity = 1
                }
            ]
        };

        var result = await service.RouteAsync(request);

        result.Feasible.Should().BeTrue();
        result.Routing.Should().HaveCount(2);
    }

    [Fact]
    public async Task RouteAsync_ShouldUseMailOrder_WhenMailOrderIsAllowed()
    {
        var service = CreateService(
            suppliers:
            [
                new Supplier
                {
                    SupplierId = "SUP-MAIL",
                    SupplierName = "Mail Supplier",
                    ServiceZips = "90001",
                    ProductCategories = ["wheelchair"],
                    CustomerSatisfactionScore = 9,
                    CanMailOrder = true
                }
            ]);

        var request = new OrderRequest
        {
            OrderId = "ORD-TEST",
            CustomerZip = "10015",
            MailOrder = true,
            Items =
            [
                new OrderItem
                {
                    ProductCode = "WC-STD-001",
                    Quantity = 1
                }
            ]
        };

        var result = await service.RouteAsync(request);

        result.Feasible.Should().BeTrue();
        result.Routing[0]
            .Items[0]
            .FulfillmentMode
            .Should()
            .Be("mail_order");
    }

    [Fact]
    public async Task RouteAsync_ShouldRejectMailOrderSupplier_WhenMailOrderIsFalse()
    {
        var service = CreateService(
            suppliers:
            [
                new Supplier
                {
                    SupplierId = "SUP-MAIL",
                    SupplierName = "Mail Supplier",
                    ServiceZips = "90001",
                    ProductCategories = ["wheelchair"],
                    CustomerSatisfactionScore = 9,
                    CanMailOrder = true
                }
            ]);

        var request = new OrderRequest
        {
            OrderId = "ORD-TEST",
            CustomerZip = "10015",
            MailOrder = false,
            Items =
            [
                new OrderItem
                {
                    ProductCode = "WC-STD-001",
                    Quantity = 1
                }
            ]
        };

        var result = await service.RouteAsync(request);

        result.Feasible.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("No eligible supplier found"));
    }

    [Fact]
    public async Task RouteAsync_ShouldMergeDuplicateProductCodes()
    {
        var service = CreateService();

        var request = new OrderRequest
        {
            OrderId = "ORD-TEST",
            CustomerZip = "10015",
            MailOrder = false,
            Items =
            [
                new OrderItem
                {
                    ProductCode = "WC-STD-001",
                    Quantity = 1
                },
                new OrderItem
                {
                    ProductCode = "WC-STD-001",
                    Quantity = 2
                }
            ]
        };

        var result = await service.RouteAsync(request);

        result.Feasible.Should().BeTrue();

        var routedItem = result.Routing
            .SelectMany(route => route.Items)
            .Single(item => item.ProductCode == "WC-STD-001");

        routedItem.Quantity.Should().Be(3);
    }

    [Fact]
    public async Task RouteAsync_ShouldPreferHigherRatedSupplier_WhenMultipleSuppliersCanFulfill()
    {
        var service = CreateService(
            suppliers:
            [
                new Supplier
            {
                SupplierId = "SUP-LOW",
                SupplierName = "Lower Rated Supplier",
                ServiceZips = "10001-10100",
                ProductCategories = ["wheelchair"],
                CustomerSatisfactionScore = 6,
                CanMailOrder = false
            },
            new Supplier
            {
                SupplierId = "SUP-HIGH",
                SupplierName = "Higher Rated Supplier",
                ServiceZips = "10001-10100",
                ProductCategories = ["wheelchair"],
                CustomerSatisfactionScore = 9,
                CanMailOrder = false
            }
            ]);

        var request = new OrderRequest
        {
            OrderId = "ORD-TEST",
            CustomerZip = "10015",
            MailOrder = false,
            Items =
            [
                new OrderItem
            {
                ProductCode = "WC-STD-001",
                Quantity = 1
            }
            ]
        };

        var result = await service.RouteAsync(request);

        result.Feasible.Should().BeTrue();
        result.Routing[0].SupplierId.Should().Be("SUP-HIGH");
    }

    [Fact]
    public async Task RouteAsync_ShouldPreferLocalSupplier_WhenMailOrderSupplierRatingIsSimilar()
    {
        var service = CreateService(
            suppliers:
            [
                new Supplier
            {
                SupplierId = "SUP-MAIL",
                SupplierName = "Mail Supplier",
                ServiceZips = "90001",
                ProductCategories = ["wheelchair"],
                CustomerSatisfactionScore = 9,
                CanMailOrder = true
            },
            new Supplier
            {
                SupplierId = "SUP-LOCAL",
                SupplierName = "Local Supplier",
                ServiceZips = "10001-10100",
                ProductCategories = ["wheelchair"],
                CustomerSatisfactionScore = 8.5m,
                CanMailOrder = false
            }
            ]);

        var request = new OrderRequest
        {
            OrderId = "ORD-TEST",
            CustomerZip = "10015",
            MailOrder = true,
            Items =
            [
                new OrderItem
            {
                ProductCode = "WC-STD-001",
                Quantity = 1
            }
            ]
        };

        var result = await service.RouteAsync(request);

        result.Feasible.Should().BeTrue();
        result.Routing[0].SupplierId.Should().Be("SUP-LOCAL");
    }

    private static OrderRoutingService CreateService(bool includeConsolidatedSupplier = true, IReadOnlyList<Supplier>? suppliers = null)
    {
        var products = new Dictionary<string, Product>(StringComparer.OrdinalIgnoreCase)
        {
            ["WC-STD-001"] = new()
            {
                ProductCode = "WC-STD-001",
                Category = "standard wheelchair"
            },
            ["OX-PORT-024"] = new()
            {
                ProductCode = "OX-PORT-024",
                Category = "portable oxygen concentrator"
            }
        };

        suppliers ??= BuildSuppliers(includeConsolidatedSupplier);

        return new OrderRoutingService(
            new StubProductRepository(products),
            new StubSupplierRepository(suppliers),
            new ZipCoverageService(),
            NullLogger<OrderRoutingService>.Instance);
    }

    private static IReadOnlyList<Supplier> BuildSuppliers(bool includeConsolidatedSupplier)
    {
        var suppliers = new List<Supplier>
        {
            new()
            {
                SupplierId = "SUP-WHEEL",
                SupplierName = "Wheelchair Supplier",
                ServiceZips = "10001-10100",
                ProductCategories = ["wheelchair"],
                CustomerSatisfactionScore = 8,
                CanMailOrder = false
            },
            new()
            {
                SupplierId = "SUP-OXYGEN",
                SupplierName = "Oxygen Supplier",
                ServiceZips = "10001-10100",
                ProductCategories = ["oxygen"],
                CustomerSatisfactionScore = 8,
                CanMailOrder = false
            }
        };

        if (includeConsolidatedSupplier)
        {
            suppliers.Add(new Supplier
            {
                SupplierId = "SUP-001",
                SupplierName = "Consolidated Supplier",
                ServiceZips = "10001-10100",
                ProductCategories = ["wheelchair", "oxygen"],
                CustomerSatisfactionScore = 7,
                CanMailOrder = false
            });
        }

        return suppliers;
    }
}