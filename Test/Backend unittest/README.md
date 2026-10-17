# Backend unit tests

Project: `Test/Backend unittest/UbhayaFashions.Tests.csproj`

Run from the repository root:

```bash
dotnet test "Test/Backend unittest/UbhayaFashions.Tests.csproj"
```

Tests use EF Core InMemory and Moq where applicable, so covered tests should not need a live SQL Server or real payment/email services.

## Current coverage

- `ProductsController` and `CategoriesController`: selected catalogue, empty-result, validation, create/update/delete cases.
- `AuthController` and `AdminAuthController`: selected invalid-input and credential cases.
- `AddressesController`, `OrdersController`, `PaymentsController`, `WishlistController`, `NotificationsController`, and `RecentlyViewedController`: selected authorization, missing-resource, and empty-state cases.
- `ProductCatalogTests`: colour normalization/matching and price-ceiling boundaries.

## Coverage limitation

This is a starter suite, not complete action-level coverage. Add positive, negative, boundary, authorization/ownership, persistence/side-effect, and dependency-failure tests for each applicable controller action. The suite has not been executed in this environment; run it and fix compile/runtime failures before relying on the results.
