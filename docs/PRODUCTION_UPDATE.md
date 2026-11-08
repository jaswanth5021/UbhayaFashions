# Production checkout update — 8 November 2026

## Apply source changes

Stop local development processes before replacing source files. Extract the update ZIP, open PowerShell in the extracted folder, and run:

```powershell
.\Apply-Update.ps1 -ProjectPath 'D:\admin\ubhaya-jaipuri-checkout-updated' -CheckOnly
.\Apply-Update.ps1 -ProjectPath 'D:\admin\ubhaya-jaipuri-checkout-updated'
```

The installer checks original and replacement SHA-256 hashes before writing anything. A conflict stops the entire update. It backs up replaced files under `.production-update-backups` and attempts to restore them if copying fails. It does not run the application, contact payment providers, deploy, or change a database. Existing secret-bearing appsettings.json files are not in this package. Existing files already matching the replacement are skipped.

## Changes

- Development-only test-payment endpoint, disabled production configuration, and production startup safeguards.
- Shared payment finalization for browser verification and signed webhooks; capture status, amount and currency remain checked.
- Per-order update locks and sorted SQL Server variant locks; combined quantities and size/colour resolution before stock changes.
- InventoryCommitted distinguishes prepaid drafts from deducted inventory. Customer cancellation restores only deducted stock once.
- Captures after cancellation are recorded as captured with refund pending; they never restart fulfilment.
- CheckoutKey plus request fingerprint and a transaction-owned application lock make repeated submissions of the same form reuse the pending provider order. Changed details require a fresh checkout form. Provider order creation and local persistence are committed before returning a payable order to the browser.
- Complete address/PIN/serviceability validation even for free shipping; fulfilment uses the stored address, not the verification request address.
- Payment and Pending follow-up state are committed together. A background worker handles shipment creation and confirmation email. Email failures retry every five minutes. Interrupted/failed courier submissions require reconciliation before resubmitting; ambiguous provider outcomes are deliberately not automatically retried.
- Existing auth tokens become invalid at rollout. Password changes/reset invalidate previously issued tokens through a password-derived HMAC version claim. Customers and admins have explicit separate API role requirements.
- Authentication rate limits; secure production cookies, trusted proxy handling, configured CORS origin, durable frontend data-protection keys, health endpoints.
- Fixed wishlist Razor compilation, populated solution, added frontend Dockerfile, ignored local secrets in Docker contexts, and added CI builds/tests.

## Configure hosting before running

Production startup now rejects enabled test payments, missing live Razorpay settings, short JWT keys, non-HTTPS FrontendUrl and missing UploadsPath. Delhivery and SMTP settings must also be configured for fulfilment.

1. Store real secrets on the host or in a secret manager. Review history and rotate any secrets previously exposed. The update does not rotate credentials or prove history is clean.
2. Copy the two `.production.env.example` files to their `.production.env` counterparts and replace every placeholder. Set SQL_SA_PASSWORD separately for compose database provisioning; provision a least-privilege application SQL account and trusted SQL certificate. Do not use the SQL admin account for normal traffic. Migration execution needs temporary DDL permissions.
3. Terminate HTTPS at the ingress proxy. Only forward public storefront traffic and the Razorpay webhook path to their appropriate services. Keep SQL and API ports private. Compose binds published service ports to loopback; if your proxy is itself containerized, connect it to the service network instead.
4. Set exact `ReverseProxy:KnownProxies` addresses. The frontend trusts the ingress proxy; the API trusts the frontend and the ingress used for webhooks. Use stable addresses. Never clear trusted-proxy lists to trust all senders. The frontend forwards its validated client IP and scheme so API rate limits apply to the shopper, not every shopper collectively.
5. Configure frontend `AllowedHosts`, API base URL, persistent uploads and a persistent data-protection key directory. Restrict access and encrypt the key storage at rest. Back up uploads, keys and SQL data and test restoration.
6. Set both applications to Production. Configure public webhook delivery over HTTPS. `GET /health/live` tests process availability; API `GET /health/ready` checks SQL connectivity. Add external alerts for webhook rejection, payment review, courier review and email retries. Rate limits are per process; use ingress/distributed controls when scaling across replicas.

## Database deployment

Back up the database and stop application traffic/workers. Review migration `20261108120000_ProductionCheckoutSafety` against a staging clone first. It adds inventory/follow-up state and checkout-attempt fields with a unique customer/attempt index. Existing non-cancelled paid/COD orders are marked inventory-committed; historical follow-ups are marked Legacy and are not automatically replayed. Reconcile historic orders/inventory before relying on that backfill.

Use the published backend executable from its publish directory with migration credentials:

```powershell
dotnet backend.dll --migrate-only
```

This controlled maintenance command applies EF migrations and the existing legacy schema compatibility SQL, then exits before starting HTTP endpoints or workers. Normal production startup checks pending migrations and refuses to serve an outdated schema. The inherited legacy compatibility SQL is not yet fully converted to versioned migrations; test this maintenance path on a database clone.

After a successful upgrade, restore least-privilege runtime credentials and start the new backend and frontend together. Do not run old and new versions concurrently against the upgraded database. A source-file backup is not a database rollback. If rollback is necessary, stop traffic and restore the coordinated application/database backup; assess any provider payments accepted since the backup before resuming.

## Operational recovery

Check Orders.FollowUpStatus:

- Pending: queued for processing.
- InProgress: claimed by a worker/admin courier operation.
- Complete: shipment recorded and confirmation call completed.
- EmailRetry: confirmation delivery will be retried.
- Review: investigate logs and saved shipment state. Search Delhivery using `UBHAYA-{orderId}` before retrying. If the provider created the parcel but the response was lost, reconcile the existing AWB; do not create another parcel.
- Legacy/Cancelled: not eligible for automatic processing.

InProgress jobs older than 15 minutes are parked in Review. A process crash immediately after an email is sent may leave its delivery uncertain; mail is at-least-once, not exactly-once. Refund Pending and Refund Initiated still require provider reconciliation; this update does not add automated refund-completion webhooks. GA4 remains best effort.

## Validation and release gate

Performed on a separate source copy: backend Release build passed; frontend Release build passed; 24 targeted checks passed, including generation of the new migration SQL without connecting to SQL Server. Existing nullable compiler warnings remain. The xUnit suite and NuGet vulnerability audit could not complete because NuGet was unreachable in the review environment. Local builds used cached dependencies with the vulnerability audit disabled; this is not a clean dependency-security result.

Run with network access:

```powershell
dotnet restore LadiesDressStore.sln
dotnet build LadiesDressStore.sln -c Release --no-restore
dotnet test tests/UbhayaFashions.Tests/UbhayaFashions.Tests.csproj -c Release
dotnet run --project tests/CheckoutSafety.SmokeTests/CheckoutSafety.SmokeTests.csproj -c Release
dotnet list LadiesDressStore.sln package --vulnerable --include-transitive
```

Before production, verify on SQL Server and provider sandbox accounts:

- Two concurrent buyers competing for the last size/colour; stock must not be oversold or overwritten.
- Duplicate cart lines, duplicate browser callbacks and duplicate/out-of-order webhooks.
- Double-click/retry with the same checkout key; only one payable local/provider order returned.
- Unpaid cancellation, paid cancellation, and capture racing with cancellation.
- Invalid PIN and unserviceable destination both above and below the free-shipping threshold.
- Browser closes after capture; webhook finalizes and worker processes the order.
- Crash after payment commit; crash after courier submission; SMTP outage and recovery.
- Correct proxy scheme/client IP, secure cookies, admin/customer role isolation and old-token rejection after password reset.
- Upgrade from a production-like database, backup restore, Docker startup and health probes.

No live payment, shipment, refund, schema change, container deployment or SQL concurrency test was performed by this update session. Do not interpret passing builds/helper tests as production certification.
