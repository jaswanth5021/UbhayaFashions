<div align="center">

# Engineering Standards
### Code style and contribution conventions

**Readable code, explicit boundaries and small, verifiable changes.**

</div>

---

## 01 · General principles

- Follow the conventions already established in the surrounding code.
- Prefer simple, explicit solutions over clever abstractions.
- Keep changes focused; avoid unrelated formatting or refactoring.
- Name concepts consistently across DTOs, services, routes and UI.
- Validate input at system boundaries.
- Handle expected failures explicitly; do not swallow exceptions.
- Add tests for bug fixes and business-rule changes.

## 02 · C# and .NET

- Target the repository's .NET 8 projects unless a planned upgrade is approved.
- Use nullable reference types and resolve warnings intentionally.
- Use async APIs for I/O; propagate cancellation tokens where supported.
- Use `PascalCase` for public types and members; `camelCase` for locals and parameters.
- Keep controllers thin and move reusable business logic into services.
- Use dependency injection instead of constructing infrastructure dependencies inside controllers.
- Prefer DTOs for request and response contracts.
- Use `decimal` for currency.
- Avoid exposing EF Core entities directly from API endpoints.
- Do not introduce synchronous blocking calls such as `.Result` or `.Wait()` in request paths.

## 03 · API conventions

- Use clear resource-oriented routes and correct HTTP methods/status codes.
- Validate DTOs and return actionable, non-sensitive validation errors.
- Keep authentication and authorization explicit.
- Make payment, webhook and shipping handlers idempotent where possible.
- Use consistent response shapes and avoid leaking internal exception details.
- Document configuration changes and endpoint behaviour.

## 04 · Razor, HTML, CSS and JavaScript

- Use semantic HTML and accessible labels.
- Keep Razor views focused on presentation.
- Prefer shared partials/components for repeated UI.
- Scope CSS to components where practical; avoid broad selectors that cause regressions.
- Use responsive layouts rather than fixed pixel widths.
- Keep JavaScript modular and avoid duplicate event handlers.
- Do not rely on hover for essential touch interactions.
- Provide loading, empty, disabled and error states.

## 05 · Naming and file organisation

| Item | Convention | Example |
|---|---|---|
| C# class | PascalCase | `OrdersController.cs` |
| Interface | I + PascalCase | `IEmailService.cs` |
| Method | PascalCase | `CreateShipmentAsync` |
| Local variable | camelCase | `orderId` |
| Constant | PascalCase | `MaximumUploadSize` |
| CSS class | descriptive kebab-case | `.product-card__price` |
| Test | behaviour-focused | `CreateOrder_WhenPaymentFails_DoesNotReduceStock` |

## 06 · Error handling and observability

- Return errors that help the caller recover without exposing internals.
- Log exceptions once at the appropriate boundary with correlation context.
- Do not log passwords, tokens, full addresses or payment secrets.
- Distinguish validation errors, authorization failures, provider failures and unexpected faults.
- Never report a provider action as successful until the result has been confirmed.

## 07 · Git workflow

1. Start from an up-to-date base branch.
2. Use a short branch name describing the change.
3. Make small commits with imperative messages.
4. Run relevant tests and build checks.
5. Review the full diff for secrets, generated files and unrelated changes.
6. Open a pull request with purpose, implementation notes, tests and screenshots for UI changes.
7. Update documentation when behaviour or operations change.

## 08 · Pull request checklist

- [ ] Scope is clear and focused.
- [ ] Naming and patterns match neighbouring code.
- [ ] Validation and authorization are server-side.
- [ ] Error and retry paths are considered.
- [ ] Tests added or updated.
- [ ] Build and tests pass.
- [ ] Responsive/accessibility checks completed for UI changes.
- [ ] Configuration and documentation updated.
- [ ] No secrets, personal data or generated build output included.
