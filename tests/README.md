# Ubhaya Fashions automated tests

The test suite is split by application layer:

- `backend/UbhayaFashions.Backend.Tests`: NUnit tests for backend domain behavior and API controller behavior.
- `frontend/UbhayaFashions.Frontend.Tests`: NUnit tests for frontend models, color handling, MVC actions, and validation.
- `UbhayaFashions.Tests`: the existing xUnit regression suite. It is retained and included in the combined run.

## One-command execution (Windows)

From the repository root:

```powershell
.	estsun-all-tests.bat
```

Or:

```powershell
.	estsun-all-tests.ps1 -Configuration Release
```

The script runs the backend NUnit suite, frontend NUnit suite, existing regression suite, and a frontend Release build. It returns exit code 1 if any suite/build fails or if a test report is missing. Use that exit code in deployment automation; only exit code 0 should permit deployment.

## Reports

Results are written to `tests/TestResults`:

- TRX results for each suite.
- Cobertura XML coverage files from Coverlet.
- `summary.txt` and `summary.html`.

Coverage is collected, but this initial baseline does not claim an 80% threshold. Set a threshold only after the critical business paths and integration test environment have been comprehensively covered.

## Safety

Tests use EF Core InMemory for isolated backend controller tests and do not call live Razorpay, Delhivery, email, or production databases. Add future integration tests with dedicated test credentials and test-only services. Never put production secrets in test configuration.

## Current coverage and next expansion

The baseline tests cover product color normalization/matching, price-boundary behavior, product lookup success/not-found behavior, frontend product view-model calculations, frontend color parsing/formatting, contact form validation, and contact MVC action outcomes.

Expand the suites alongside the remaining account, checkout, inventory, order, Razorpay signature, Delhivery, admin authorization, and browser workflows. External payment/shipping/email calls must be mocked. Browser-level UI tests should run against a dedicated test host and never against production.
