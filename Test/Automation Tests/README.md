# UBHAYA Fashions automated tests

This repository includes an initial automated smoke suite and a broader test-case catalogue.

## What is included

- API smoke tests using `HttpClient` and xUnit for product listing, colors, price bounds, unknown product IDs, related products and common product-list query parameters.
- Browser UI smoke tests using Playwright for .NET and xUnit for the home, products, categories, login and cart pages, plus a basic mobile viewport overflow check.
- GitHub Actions workflow to run existing backend/frontend unit tests on pushes and pull requests. API and browser smoke jobs run when their target URLs are configured.
- [Full UBHAYA Fashions test-case catalogue](../Test%20Cases/UBHAYA-Test-Cases.md), including product discovery, account, cart, wishlist, checkout, orders, admin, security, responsive design and deployment checks.

The catalogue includes manual/next-automation scenarios as well as currently automated cases. It does not claim that every business workflow is already automated.

## Requirements

- .NET 8 SDK
- The MVC website and backend API must be running and reachable for smoke tests.
- Playwright Chromium for browser tests.
- Use staging/test accounts and test data for tests that change state.

## Configure URLs in PowerShell

Set the backend API root URL (not the MVC website URL):

```powershell
$env:UBHAYA_API_BASE_URL = "https://your-api-host/"
```

Set the MVC website URL:

```powershell
$env:UBHAYA_UI_BASE_URL = "https://your-website-host/"
```

Use actual URLs for your environment. The API base URL should resolve endpoints such as `api/products` and `api/products/colors`.

## Restore, build and install Chromium

From the repository root:

```powershell
dotnet restore "Test/Automation Tests/UbhayaFashions.Automation.Tests.csproj"
dotnet build "Test/Automation Tests/UbhayaFashions.Automation.Tests.csproj"
pwsh "Test/Automation Tests/bin/Debug/net8.0/playwright.ps1" install chromium
```

On Windows PowerShell, if `pwsh` is unavailable, install PowerShell 7 or run the generated `playwright.ps1` script with the PowerShell executable available on your machine.

## Run API tests

```powershell
dotnet test "Test/Automation Tests/UbhayaFashions.Automation.Tests.csproj" --filter FullyQualifiedName~ApiSmokeTests
```

## Run UI tests

```powershell
dotnet test "Test/Automation Tests/UbhayaFashions.Automation.Tests.csproj" --filter FullyQualifiedName~WebsiteSmokeTests
```

## Run all automation tests

Set both environment variables first, then run:

```powershell
dotnet test "Test/Automation Tests/UbhayaFashions.Automation.Tests.csproj"
```

## GitHub Actions

The workflow at `.github/workflows/automated-tests.yml` always runs existing unit-test projects. API and UI smoke-test jobs run when the corresponding repository Actions variables are configured:

- `UBHAYA_API_BASE_URL`
- `UBHAYA_UI_BASE_URL`

Configure these under **Settings → Secrets and variables → Actions → Variables**. GitHub-hosted runners must be able to reach those URLs. If the app is hosted only on a private server/network, use a suitably secured self-hosted runner or a reachable staging environment.

## Safety and coverage notes

- Prefer staging/test deployments. API smoke tests use read-only GET requests.
- Do not automate real customer purchases or use live payment credentials in CI.
- Checkout, payment, admin, account mutation and inventory scenarios in the catalogue require isolated test data and additional workflow-specific setup before they can be safely automated.
- Keep HTTPS certificate validation enabled and use a trusted certificate.
- The tests have not been executed against your deployed application from this session. Review the first GitHub Actions run and adjust target URLs/routes if needed.
