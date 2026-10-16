# Unit test suite

Run the suite from the repository root:

```bash
dotnet test tests/UbhayaFashions.Tests/UbhayaFashions.Tests.csproj
```

Controller tests use a uniquely named EF Core InMemory database per test so tests do not need a running SQL Server and do not share state.

## Current controller coverage

| Controller | Covered actions | Cases included |
| --- | --- | --- |
| `CategoriesController` | `All`, `Home` | sorted results, empty results, home-page filtering, four-item limit |
| `ProductsController` | `GetAll`, `GetColors`, `GetPriceBounds`, `Get`, `GetRelated`, `GetNewarrivals`, `Create`, `Update`, `Delete` | empty results, canonical/distinct colors, default price bounds, missing IDs, invalid categories, successful create/update/delete |
| `ProductCatalogTests` | `ProductColorCatalog.Normalize`, `Matches`, `PriceCeiling` | canonicalization, duplicates, whole-name matching, fallback and high-price boundaries |

## Coverage principles

For each controller action, add applicable tests for:

- expected success/status code and response payload;
- missing or unknown resources;
- invalid input and validation boundaries;
- empty collections and optional query parameters;
- authorization and identity/ownership boundaries;
- dependency/database failures and exception behavior;
- asynchronous execution and side effects (database changes, stock, emails, or payment calls).

Not every category applies to every action. Authentication, order/payment workflows, and external-service calls should use dedicated fakes/mocks and should not contact real payment or email services.

## Important limitation

This is the first controller-test batch. Auth, order, payment, address, wishlist, admin, notification, and other controller actions still need their own test classes before the repository can be considered comprehensively covered. The current suite has not been executed in this environment; run the command above and resolve any provider-specific behavior before relying on the results.
