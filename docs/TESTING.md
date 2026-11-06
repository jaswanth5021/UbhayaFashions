# Testing guide

This guide describes the available automated test project and useful manual journeys. It does not report a test run.

## Automated tests

Run these commands from the repository root:

```powershell
dotnet build LadiesDressStore.sln
dotnet test tests/UbhayaFashions.Tests/UbhayaFashions.Tests.csproj
```

The current xUnit project contains tests for product color normalization/matching and price-ceiling behavior. It does not currently represent broad API, browser, payment-provider, or accessibility integration coverage.

## Recommended test layers

The diagram shows a recommended coverage mix, not the current automated test inventory.

```mermaid
%%{init: {"theme":"base","themeVariables":{"background":"#fbf8fc","primaryColor":"#fff7f5","primaryTextColor":"#302522","primaryBorderColor":"#68142e","lineColor":"#68142e","edgeLabelBackground":"#fbf8fc","actorBkg":"#fff7f5","actorBorder":"#68142e","actorTextColor":"#302522","actorLineColor":"#68142e","signalColor":"#68142e","signalTextColor":"#302522","labelBoxBkgColor":"#fff7f5","labelBoxBorderColor":"#68142e","labelTextColor":"#302522","loopTextColor":"#302522","noteBkgColor":"#fff8e9","noteBorderColor":"#c69a45","noteTextColor":"#302522","activationBkgColor":"#f8e9e9","activationBorderColor":"#68142e","sequenceNumberColor":"#302522"}}}%%
flowchart TD
    U[Unit tests: catalog and business rules] --> I[Integration tests: API, database and providers]
    I --> E[End-to-end tests: checkout, payment and fulfilment]
    U -.many fast checks.-> R[Focused coverage for revenue-critical journeys]
    I -.targeted.-> R
    E -.few but complete.-> R
```

## Manual smoke-test flow

```mermaid
%%{init: {"theme":"base","themeVariables":{"background":"#fbf8fc","primaryColor":"#fff7f5","primaryTextColor":"#302522","primaryBorderColor":"#68142e","lineColor":"#68142e","edgeLabelBackground":"#fbf8fc","actorBkg":"#fff7f5","actorBorder":"#68142e","actorTextColor":"#302522","actorLineColor":"#68142e","signalColor":"#68142e","signalTextColor":"#302522","labelBoxBkgColor":"#fff7f5","labelBoxBorderColor":"#68142e","labelTextColor":"#302522","loopTextColor":"#302522","noteBkgColor":"#fff8e9","noteBorderColor":"#c69a45","noteTextColor":"#302522","activationBkgColor":"#f8e9e9","activationBorderColor":"#68142e","sequenceNumberColor":"#302522"}}}%%
flowchart TD
    A[Start SQL Server and API] --> B[Start storefront]
    B --> C[Browse home, catalog and product detail]
    C --> D[Check mobile and keyboard navigation]
    D --> E[Sign in and manage address]
    E --> F{Checkout path}
    F -->|Test Razorpay| G[Verify order/payment and stock]
    F -->|COD| H[Verify COD order and stock]
    G --> I[Inspect email and tracking/shipment state]
    H --> I
    I --> J[Check robots, sitemap and product feed]
```

## Recommended focused checks

- Catalog: missing product, unavailable variant, zero stock, and invalid quantity.
- Account: signup, login, optional email verification, reset expiry and one-time use.
- Address: add/edit/delete, default selection, ownership, and location permission denied.
- Payment: signature mismatch, unpaid status, amount/order mismatch, retry, webhook duplicate, and inventory race.
- Shipping: missing configuration, provider rejection, timeout/possible duplicate, and admin retry.
- SEO: `/robots.txt`, `/llms.txt`, `/sitemap.xml`, and `/feeds/google-products.xml`.
- Accessibility: keyboard-only use, focus visibility, labels, contrast, zoom/reflow, and screen reader announcements.
- Responsive behavior: test common narrow mobile and desktop widths for overflow and clipped controls.

Use Razorpay test mode and Delhivery staging for provider checks. Never create a real customer payment to smoke-test local code. Record environment, URL, device profile, and actual output when sharing results.
