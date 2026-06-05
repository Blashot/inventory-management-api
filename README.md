# Inventory Management API

[![CI](https://github.com/Blashot/inventory-management-api/actions/workflows/build.yml/badge.svg)](https://github.com/Blashot/inventory-management-api/actions/workflows/build.yml)

A .NET 10 Inventory Management API supporting product management, stock tracking, order processing and discount calculation, built using Clean Architecture, Vertical Slice Architecture and CQRS.

---

## Architecture Overview

```text
src/
 ├─ SharedKernel      → Entity, Result, Error, IDateTimeProvider, IDomainEvent
 ├─ Domain            → Products, Customers, Orders (aggregates, domain rules, errors)
 ├─ Application       → CQRS handlers, validators, pricing service, abstractions
 ├─ Infrastructure    → EF Core, PostgreSQL, HolidayCalendar, Serilog, health checks
 └─ Web.Api           → Controllers, Swagger, exception handling, program entry point

tests/
 ├─ Domain.UnitTests                 → pure domain logic, no mocks
 ├─ Application.UnitTests            → handlers and pricing service (NSubstitute mocks)
 ├─ Infrastructure.IntegrationTests  → real PostgreSQL via Testcontainers
 └─ ArchitectureTests                → Clean Architecture boundary enforcement
```

### Layer Rules

| Layer          | May depend on         |
| -------------- | --------------------- |
| Domain         | SharedKernel only     |
| Application    | Domain + SharedKernel |
| Infrastructure | Application           |
| Web.Api        | Infrastructure        |

### CQRS

Commands and queries each live in their own folder (Vertical Slice). Every feature contains:

* `Command` / `Query`
* `Handler`
* `Validator`

---

## Domain Model

### Product

* `Id`
* `Name` (max 50)
* `Description` (max 50)
* `Price` (> 0)
* `Stock` (>= 0)

Business rules:

* Name is required.
* Description is required.
* Price must be greater than zero.
* Stock cannot be negative.
* Stock cannot become negative after reduction.

### Customer

* `Id`
* `Name`
* `Region` (`US` | `Europe` | `Asia`)

### Order

* `Id`
* `CustomerId`
* `OrderLines`
* `TotalAmount`
* `DiscountApplied`
* `CreatedAt`

Business rules:

* Order must contain at least one order line.
* Creating an order reduces stock transactionally.
* Order stores pricing and discount information at purchase time.

### OrderLine

Owned by `Order`.

Stores:

* `ProductId`
* `ProductName`
* `UnitPrice`
* `Quantity`

This acts as a historical snapshot and is not affected by future product changes.

---

## Pricing & Discount Rules

### Regional Price Adjustment (applied first)

| Region | Multiplier |
| ------ | ---------- |
| US     | × 1.00     |
| Europe | × 1.15     |
| Asia   | × 1.05     |

### Discount Policies

Discounts are never combined.

The customer always receives the most beneficial applicable discount.

| Policy          | Rule                                                         |
| --------------- | ------------------------------------------------------------ |
| Volume Discount | ≥5 units → 10%, ≥10 units → 20%, ≥50 units → 30%             |
| Black Friday    | 25% off the entire order                                     |
| Holiday Sale    | 15% off the most expensive product on Polish public holidays |

`OrderPricingService` evaluates all applicable discount policies and selects the discount producing the highest customer benefit.

---

## API Endpoints

| Method | Path         | Description           |
| ------ | ------------ | --------------------- |
| POST   | `/products`  | Create a product      |
| GET    | `/products`  | Retrieve all products |
| POST   | `/orders`    | Create an order       |
| POST   | `/customers` | Create a customer     |
| GET    | `/health`    | Health check          |

Swagger UI is available at `/swagger` when running in Development mode.

---

## Prerequisites

* .NET 10 SDK
* Docker Desktop

---

## Configuration

The repository includes:

* `.env.example` – Docker Compose configuration template
* `appsettings.Development.Example.json` – local development configuration template

Before running the application, create local configuration files:

```bash
cp .env.example .env

cp src/Web.Api/appsettings.Development.Example.json \
   src/Web.Api/appsettings.Development.json
```

`appsettings.Development.json` is intentionally excluded from source control via `.gitignore`.

---

## Running with Docker

```bash
cp .env.example .env

docker compose up -d
```

Available services:

```text
API:      http://localhost:5000
Swagger:  http://localhost:5000/swagger
Seq:      http://localhost:8081
```

---

## Running Locally

Start infrastructure services:

```bash
docker compose up postgres seq -d
```

Run the API:

```bash
cd src/Web.Api
dotnet run
```

EF Core migrations are applied automatically during startup in Development mode.

### Adding Migrations

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Infrastructure \
  --startup-project src/Web.Api \
  --output-dir Database/Migrations
```

---

## Running Tests

Run all tests:

```bash
dotnet test
```

Run specific test projects:

```bash
dotnet test tests/Domain.UnitTests
dotnet test tests/Application.UnitTests
dotnet test tests/ArchitectureTests
dotnet test tests/Infrastructure.IntegrationTests
```

> Integration tests use Testcontainers and require Docker.

---

## Testing Strategy

| Test Suite                      | Purpose                                                          | Tools                           |
| ------------------------------- | ---------------------------------------------------------------- | ------------------------------- |
| Domain.UnitTests                | Entity invariants, stock reduction, pricing rules, domain events | xUnit, Shouldly                 |
| Application.UnitTests           | Handlers, pricing service, discount selection                    | xUnit, Shouldly, NSubstitute    |
| Infrastructure.IntegrationTests | Persistence, stock reduction, order workflows                    | xUnit, Testcontainers, Shouldly |
| ArchitectureTests               | Layer dependency validation                                      | NetArchTest, Shouldly           |

---

## Technical Decisions

* Clean Architecture for separation of concerns.
* Vertical Slice Architecture for feature-based organization.
* CQRS for command/query separation.
* Result pattern for business failures.
* FluentValidation for input validation.
* PostgreSQL as the primary datastore.
* Testcontainers for repeatable integration tests.
* Policy-based pricing engine following the Open/Closed Principle.

---

## Assumptions

1. Volume discounts are calculated using the total quantity across all order lines.
2. Holiday Sale applies only to the most expensive product line.
3. Customers must be created before placing an order.
4. No authentication is implemented because it was not required by the assignment.
5. All timestamps are stored in UTC via `IDateTimeProvider`.
6. Holiday calculations use a deterministic implementation of Polish public holidays.

---

## Simplifications

* Products can only be created and retrieved.
* Product update and delete operations are not implemented.
* Product listing does not support pagination or filtering.
* Holiday calendar rules are implemented in code.
* API versioning is intentionally omitted.

---

## Trade-offs

| Decision                                                    | Rationale                                                                           |
| ----------------------------------------------------------- | ----------------------------------------------------------------------------------- |
| `OrderLine.ProductId` has no foreign key constraint         | Preserves historical order integrity even if products change later                  |
| Domain events are dispatched after `SaveChangesAsync`       | Simpler eventual consistency model appropriate for this scope                       |
| `IOrderPricingService` lives in Application                 | Pricing requires abstractions such as holiday calculation while keeping Domain pure |
| Discount policies are implemented through `IDiscountPolicy` | Supports Open/Closed Principle and easy extensibility                               |

---

## Future Improvements

* Product pagination and filtering.
* Product update and delete endpoints.
* Authentication and authorization.
* External holiday provider integration.
* API versioning for multiple clients.
* Caching of frequently accessed data.
* Outbox Pattern for asynchronous integrations.
* Additional discount strategies and promotional campaigns.
