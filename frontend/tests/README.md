# Frontend unit-test workspace

The production MVC website is the existing `frontend/` project at the repository root. Its controller test files belong here, separate from API controller tests:

- `frontend/tests/Controllers/HomeControllerTests.cs`
- `frontend/tests/Controllers/ProductsControllerTests.cs`
- `frontend/tests/Controllers/AccountControllerTests.cs`
- `frontend/tests/Controllers/CartControllerTests.cs`
- `frontend/tests/Controllers/OrdersControllerTests.cs`
- `frontend/tests/Controllers/ContactControllerTests.cs`
- `frontend/tests/Controllers/AdminControllerTests.cs`
- `frontend/tests/Controllers/WishlistControllerTests.cs`

These tests should reference the actual frontend project and mock `ApiService`, `AdminApiService`, `IHttpClientFactory`, session, cookies, and authentication as applicable. Do not copy backend tests into this project: the MVC controllers have different behavior and dependencies.

A frontend test project should be created after confirming the exact project file name and its target framework. This folder is intentionally documentation-only until that reference is verified.
