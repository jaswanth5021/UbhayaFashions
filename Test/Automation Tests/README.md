# UBHAYA Fashions automated tests

This project contains:
- API smoke tests using `HttpClient` and xUnit.
- Browser UI smoke tests using Playwright for .NET and xUnit.

## Requirements
- .NET 8 SDK
- The MVC website and backend API must be running and reachable.
- Playwright Chromium for browser tests.

## Configure URLs in PowerShell

Set the backend API root URL (not the MVC website URL):

```powershell
$env:UBHAYA_API_BASE_URL = "https://your-api-host/"
```

Set the MVC website URL:

```powershell
$env:UBHAYA_UI_BASE_URL = "https://your-website-host/"
```

Use the actual URLs for your environment. The API base URL should resolve endpoints such as `api/products` and `api/products/colors`.

## Restore, build, and install Chromium

From the repository root:

```powershell
dotnet restore "Test/Automation Tests/UbhayaFashions.Automation.Tests.csproj"
dotnet build "Test/Automation Tests/UbhayaFashions.Automation.Tests.csproj"
pwsh "Test/Automation Tests/bin/Debug/net8.0/playwright.ps1" install chromium
```

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

Configure these under **Settings → Secrets and variables → Actions → Variables**. GitHub-hosted runners must be able to reach those URLs.

## Safety notes
- Prefer a staging/test deployment for automated tests.
- The included API tests use read-only GET endpoints; they do not create orders or change customer data.
- Do not use live payment credentials or automate real customer purchases in smoke tests.
- Keep HTTPS certificate validation enabled. Use a trusted certificate on the target server.

These are initial smoke tests, not complete coverage of all manual test cases. Expand them with assertions for expected page content, product data, login, cart, checkout, and order workflows as test data and environment are established.
