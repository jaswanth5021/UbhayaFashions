<div align="center">

# AI Agent & Contributor Guide
### Operating rules for safe changes in UBHAYA FASHIONS

**Read this before exploring, editing, testing or deploying the codebase.**

</div>

---

## 01 · Mission

Improve UBHAYA FASHIONS while preserving existing customer journeys, business rules, data integrity and production stability.

## 02 · Repository map

- `frontend/` — ASP.NET Core MVC storefront and admin website.
- `backend/` — ASP.NET Core Web API and business logic.
- `backend/Data/` — EF Core database context.
- `backend/Migrations/` — database migrations.
- `tests/` — automated tests.
- `docs/` — product and engineering documentation.
- Root setup guides — operational instructions for admin, checkout, Razorpay and Delhivery.

## 03 · Mandatory workflow

1. Inspect the current implementation before proposing a change.
2. Read the relevant controller, service, DTO, view, model and tests.
3. Identify the source of truth and the trust boundary.
4. Make the smallest complete change that solves the problem.
5. Add or update tests for changed behaviour.
6. Build and run relevant tests.
7. Review the diff for accidental changes, secrets and generated files.
8. Update documentation when behaviour, configuration or operations change.
9. Report exactly what changed, what was tested and what remains unverified.

## 04 · Non-negotiable rules

### Architecture
- Keep SQL access in the backend.
- Frontend calls the API through existing service abstractions.
- Keep controllers thin; reuse service-layer business rules.
- Do not introduce a second source of truth for price, inventory, payment or order state.

### Security
- Never hardcode or expose secrets.
- Never move payment or Delhivery credentials into browser code.
- Enforce authorization and ownership in the backend.
- Do not weaken validation, authentication, anti-forgery or CORS settings to make a test pass.
- Do not claim certifications or compliance without evidence.

### Commerce
- Recalculate totals and validate stock on the server.
- Do not trust client-side payment success.
- Verify payment signatures and provider events.
- Make callbacks and retries idempotent.
- Do not manifest prepaid orders before payment is verified.
- Do not report shipment success without a confirmed carrier response.

### UI
- Preserve the existing brand identity unless a redesign is requested.
- Test narrow mobile and desktop layouts.
- Prevent horizontal overflow.
- Keep essential interactions available without hover.
- Preserve accessible labels, focus and feedback states.
- Avoid large visual changes unrelated to the requested task.

### Data and deployment
- Never delete or overwrite production data as part of a code change.
- Review migrations and backup/restore implications.
- Keep uploaded media in durable storage outside ephemeral publish folders.
- Do not run production migrations or deployment commands without explicit approval.

## 05 · Validation commands

```bash
dotnet build LadiesDressStore.sln
dotnet test tests/UbhayaFashions.Tests/UbhayaFashions.Tests.csproj
```

Run commands from the repository root. If the environment lacks .NET, Docker or external provider access, report that limitation rather than claiming the check passed.

## 06 · Definition of done

- [ ] Requirement understood and existing behaviour inspected.
- [ ] Implementation follows project conventions.
- [ ] Authorization, validation and failure paths covered.
- [ ] Tests added/updated and run where possible.
- [ ] Build succeeds.
- [ ] No secrets or personal data introduced.
- [ ] UI checked at mobile and desktop sizes when applicable.
- [ ] Documentation/configuration updated.
- [ ] Remaining risks and unverified steps clearly reported.

## 07 · Communication format

When finishing a task, summarize:

1. **Changed:** files and behaviour.
2. **Why:** problem addressed.
3. **Validation:** exact build/test commands and outcomes.
4. **Risk:** migrations, configuration, deployment or provider dependencies.
5. **Next action:** only the steps that still require a human or environment access.

Do not say a task is deployed, live, secure or fully tested unless there is evidence.
