# Frontend unit tests

Project: `Test/Frontend unittest/LadiesDressStore.Web.Tests.csproj`

The tests target the ASP.NET Core MVC website in `frontend/` and are kept separate from backend API tests.

Run from the repository root:

```bash
dotnet test "Test/Frontend unittest/LadiesDressStore.Web.Tests.csproj"
```

## Current test files

- `HomeControllerTests.cs`: informational pages, not-found page, and error response.
- `ContactControllerTests.cs`: GET view, invalid model state, and successful submission.
- `AccountControllerTests.cs`: selected account-page GET actions and return URL handling.
- `CartControllerTests.cs`: empty cart.
- `OrdersControllerTests.cs`: orders-tab redirect.
- `WishlistControllerTests.cs`: anonymous visitor redirected to login.

## Coverage limitation

This is a starter suite, not complete coverage of all frontend controller actions. Add applicable positive, negative, boundary, authorization, and API-dependency failure cases. The suite has not been executed in this environment; run it and resolve compile/runtime failures before relying on the results.
