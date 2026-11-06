# Code style

Use the conventions of the surrounding code and keep changes focused. The projects target .NET 8 with nullable reference types enabled.

## C# and API

- Use async I/O and pass cancellation tokens where the surrounding API supports them.
- Keep controllers focused; put reusable provider or domain work in services.
- Use DTOs for API request and response contracts rather than exposing EF entities.
- Use `decimal` for money and validate business values on the backend.
- Prefer dependency injection and avoid blocking calls such as `.Result` or `.Wait()` in request paths.
- Keep customer ownership and admin role checks on the server.
- Return useful HTTP status codes without exposing secrets or unnecessary internals.

## Naming

| Item | Convention | Example |
|---|---|---|
| Types and public members | PascalCase | `OrdersController` |
| Interfaces | `I` plus PascalCase | `IEmailService` |
| Methods | PascalCase with async suffix for Tasks | `CreateShipmentAsync` |
| Locals and parameters | camelCase | `orderId` |
| CSS classes | kebab-case | `.product-card__price` |
| Tests | Describe behavior and condition | `PriceCeiling_UsesFallbackWhenEmpty` |

## Frontend and accessibility

Use semantic HTML and labels, preserve keyboard operation and visible focus, and provide accessible names for icon-only controls. Keep JavaScript modular and avoid duplicate event handlers. Keep CSS responsive and avoid introducing horizontal overflow at narrow widths.

## Contribution workflow

```mermaid
%%{init: {"theme":"base","themeVariables":{"background":"#fbf8fc","primaryColor":"#fff7f5","primaryTextColor":"#302522","primaryBorderColor":"#68142e","lineColor":"#68142e","edgeLabelBackground":"#fbf8fc","actorBkg":"#fff7f5","actorBorder":"#68142e","actorTextColor":"#302522","actorLineColor":"#68142e","signalColor":"#68142e","signalTextColor":"#302522","labelBoxBkgColor":"#fff7f5","labelBoxBorderColor":"#68142e","labelTextColor":"#302522","loopTextColor":"#302522","noteBkgColor":"#fff8e9","noteBorderColor":"#c69a45","noteTextColor":"#302522","activationBkgColor":"#f8e9e9","activationBorderColor":"#68142e","sequenceNumberColor":"#302522"}}}%%
flowchart TD
    A[Inspect adjacent code and requirements] --> B[Make a focused change]
    B --> C[Review validation and error paths]
    C --> D[Update docs/configuration when behavior changes]
    D --> E[Run relevant build/tests/manual checks]
    E --> F[Review the diff for secrets and unrelated edits]
    F --> G[Report evidence and remaining work]
```

From the repository root, the common validation commands are:

```powershell
dotnet build LadiesDressStore.sln
dotnet test tests/UbhayaFashions.Tests/UbhayaFashions.Tests.csproj
```

The current test project is small; do not infer broad integration or browser coverage from these commands.
