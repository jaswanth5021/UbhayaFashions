# Ubhaya Fashions test coverage

The repository root is already the `UbhayaFashions` project folder. The existing application is already split into `frontend/` (ASP.NET Core MVC website) and `backend/` (ASP.NET Core API). Test projects are kept separate from production source.

## Run tests

Backend/API tests:

```bash
dotnet test tests/UbhayaFashions.Tests/UbhayaFashions.Tests.csproj
```

Frontend/MVC tests:

```bash
dotnet test frontend/tests/LadiesDressStore.Web.Tests.csproj
```

Backend tests use EF Core InMemory and Moq so the covered controller tests do not require a live SQL Server or real payment/email calls. Frontend tests reference the existing MVC project; the web project explicitly excludes `frontend/tests/**/*.cs` from its own compilation.

## Current test files

### Frontend
- `HomeControllerTests.cs`: informational views, 404 page and 500 error response.
- `ContactControllerTests.cs`: GET view, invalid model, successful submission.
- `AccountControllerTests.cs`: selected GET actions and return-url handling.
- `CartControllerTests.cs`: empty cart.
- `OrdersControllerTests.cs`: orders tab redirect.
- `WishlistControllerTests.cs`: anonymous visitor redirected to login.

### Backend
- `ProductCatalogTests.cs`: color normalization/matching and price-ceiling boundaries.
- `ProductsControllerTests.cs`: empty results, colors, price bounds, missing product/category, and create/update/delete paths.
- `CategoriesControllerTests.cs`: sorting, empty data, homepage filtering and item limit.
- `AuthControllerTests.cs`: missing signup fields, short password and age boundaries.
- `AdminAuthControllerTests.cs`: missing credentials and unknown admin.
- `AddressesControllerTests.cs`: anonymous/invalid identity cases.
- `OrdersControllerTests.cs`: missing customer identity.
- `PaymentsControllerTests.cs`: missing customer identity for payment actions.
- `WishlistControllerTests.cs`: missing identity and unknown product.
- `NotificationsControllerTests.cs`: missing identity and unknown notifications.
- `RecentlyViewedControllerTests.cs`: missing identity, unknown product and empty history.

## Coverage status and remaining work

This is a **starter suite, not complete controller/method coverage yet**. Some controller actions currently have only a small subset of their applicable cases. Remaining work includes positive database-backed cases for addresses, orders, payments, wishlist, notifications and recently viewed; the rest of the authentication/admin-auth methods; and full action-level coverage for frontend Products and Admin controllers plus all remaining controller actions.

For every action, add applicable positive, negative, null/invalid input, boundary, not-found, authorization/ownership, persistence/side-effect, dependency failure and exception tests. Use fake HTTP handlers for frontend API calls and mocked services for payment/email integrations. Never call live Razorpay, email providers or production databases from unit tests.

The test projects have been committed but have **not been executed in this environment**. Run both commands above and fix compile/runtime failures before treating this branch as verified. Then generate coverage reports (for example with Coverlet) and use uncovered lines/branches to finish the suite.
