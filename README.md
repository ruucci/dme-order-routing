# DME Order Routing

A lightweight ASP.NET Core API that routes medical equipment orders to eligible suppliers based on feasibility, shipment consolidation, supplier quality, and geographic preference.

## Overview

This service accepts an order request and determines the best supplier routing strategy using the following priorities:

1. Feasibility  
   Only suppliers that can fulfill the requested products are considered.

2. Customer Experience  
   The router first attempts to consolidate all items into a single shipment when possible.

3. Quality  
   Higher-rated suppliers are preferred when multiple eligible suppliers exist.

4. Geographic Preference  
   When supplier ratings are similar, local suppliers are preferred over mail-order suppliers.

---

## Tech Stack

- .NET 10
- ASP.NET Core Web API
- xUnit
- FluentAssertions
- CsvHelper
- Docker

---

## Features

- Order validation for customer ZIP codes, product codes, and quantities
- Intelligent supplier routing based on fulfillment feasibility and quality
- Single-supplier consolidation when possible
- Automatic split-shipment fallback routing
- Local vs mail-order fulfillment support
- Duplicate product code merging with quantity aggregation
- Structured logging and centralized exception handling middleware
- CSV-backed repositories with in-memory caching
- Comprehensive unit test coverage
- Docker support

---

## Project Structure

```text
Dme.OrderRouting.Api
├── Configuration
├── Controllers
├── Data
├── Middleware
├── Models
├── Repositories
│   └── Interfaces
├── Services
│   └── Interfaces
└── Program.cs

Dme.OrderRouting.Tests
├── Middleware
├── Repositories
├── Services
└── TestSupport
```

---

## Running Locally

### Prerequisites

- .NET 10 SDK
- Docker Desktop (optional)

### Run API

From solution root:

```bash
dotnet run --project Dme.OrderRouting.Api
```

Swagger:

```text
http://localhost:5181/swagger
```

---

## Running with Docker

After downloading and extracting the repository zip:

```bash
cd dme-order-routing-main
```

### Build Image

```bash
docker build -t dme-order-routing ./Dme.OrderRouting.Api
```

### Run Container

```bash
docker run -p 8080:8080 dme-order-routing
```

Swagger:

```text
http://localhost:8080/swagger
```

---

## Running Tests

```bash
dotnet test
```

Run a specific test:

```bash
dotnet test --filter FullyQualifiedName~OrderRoutingServiceTests.RouteAsync_ShouldMergeDuplicateProductCodes
```

---

## Code Coverage

### Run Coverage

```bash
dotnet test --collect:"XPlat Code Coverage" --settings .coverlet.runsettings
```

### Generate HTML Report

```bash
reportgenerator -reports:"Dme.OrderRouting.Tests/TestResults/**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
```

### Open Report

```text
coverage-report/index.html
```

---

## API Endpoint

### Route Order

**POST** `/api/route`

Request body:

```json
{
  "order_id": "ORD-12345",
  "customer_zip": "10015",
  "mail_order": false,
  "items": [
    {
      "product_code": "WC-STD-001",
      "quantity": 2
    },
    {
      "product_code": "OX-PORT-024",
      "quantity": 1
    }
  ]
}
```

Example response:

```json
{
  "feasible": true,
  "routing": [
    {
      "supplier_id": "SUP-001",
      "supplier_name": "Consolidated Supplier",
      "items": [
        {
          "product_code": "WC-STD-001",
          "quantity": 2,
          "category": "wheelchair",
          "fulfillment_mode": "local"
        }
      ]
    }
  ],
  "errors": []
}
```

---

## Routing Strategy

### Single Supplier First

The router first attempts to find a single supplier that can fulfill all requested items.

If successful:
- fewer shipments
- simpler customer experience
- preferred routing outcome

### Split Shipments

If no single supplier can fulfill all items:
- the router selects the best supplier per item category
- groups items by supplier
- returns multiple shipment routes

---

## Assumptions

### Supplier ZIP Coverage

Supplier ZIP coverage is treated as business-provided reference data.

Some supplier records contain synthetic or imperfect ZIP values and broad ranges such as:

```text
00100-99999
```

The router:
- validates incoming customer ZIP codes as 5-digit numeric values
- interprets supplier ZIP ranges exactly as provided
- when `mail_order` is true, suppliers with `can_mail_order = y` may be considered even if they do not serve the customer's ZIP locally
- when `mail_order` is false, suppliers must serve the customer's ZIP
- does not validate supplier ZIP ranges against external USPS datasets

### Similar Ratings

Ratings within `1.0` are considered similar for local supplier preference.

Example:
- local supplier: 8.5
- mail-order supplier: 9.0

The local supplier is preferred because ratings are considered comparable.

### Reference Data Caching

CSV-backed repositories cache parsed supplier and product data in memory because the provided datasets are small and effectively static.

This avoids repeated file I/O and CSV parsing on every request.

In a future database-backed implementation:
- repositories would likely be scoped
- DbContext would be scoped
- caching would likely move to IMemoryCache or a distributed cache such as Redis

### Quantity Limits

The implementation validates that quantities are greater than zero.

Maximum quantity limits, inventory availability, payer restrictions, and fraud-prevention rules were intentionally not enforced because they were not specified in the assessment requirements.

---

## Design Decisions

### Repository Abstraction

Routing logic depends on interfaces rather than concrete CSV implementations:

```text
IProductRepository
ISupplierRepository
```

This allows future replacement of CSV-backed repositories with:
- database-backed repositories
- external APIs
- cached providers

without changing routing logic.

### Middleware

Custom middleware was added for:
- request and response logging
- centralized exception handling

### Structured Logging

The application uses structured logging throughout the routing pipeline and repository loading process.

---

## Future Improvements

Potential production enhancements could include:

- Database-backed repositories
- Caching zips and supplier coverage
- Supplier coverage normalization
- Inventory-aware routing
- Weighted routing heuristics
- Retry policies and resiliency
- Metrics and observability
- Background data refresh
- Geospatial routing optimization
- Buy Box rating for supplier selection
- Cleaned and normalized supplier coverage ingestion pipeline
- Background job to validate zips against USPS datasets

---

## Development Notes

- Routing logic is intentionally decoupled from CSV implementations through repository interfaces.
- Supplier ZIP coverage is evaluated dynamically from the provided reference dataset.
- CSV-backed repositories are registered as singletons because the supplied datasets are static reference data.
- In a future database-backed implementation, repositories and DbContext would likely be scoped instead.
