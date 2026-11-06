<div class="doc-hero">
<span class="doc-kicker">UBHAYA FASHIONS · ENGINEERING PRACTICE</span><span class="doc-pill">Build clean. Ship safely.</span>
<h1>Code Style Guide</h1>
<p>Shared conventions for readable C#, reliable APIs, maintainable Razor views and focused contributions.</p>
<div class="doc-meta"><div><strong>Runtime</strong>.NET 8</div><div><strong>Projects</strong>Frontend + Backend</div><div><strong>Standard</strong>Consistency first</div></div>
</div>

## <strong>01</strong> Engineering principles

<div class="doc-grid">
<div class="doc-card pink"><span class="ico">🧭</span><strong>Follow existing patterns</strong><p>Keep conventions consistent with neighbouring code.</p></div>
<div class="doc-card purple"><span class="ico">✂️</span><strong>Keep changes focused</strong><p>Avoid unrelated refactors and formatting churn.</p></div>
<div class="doc-card blue"><span class="ico">🧪</span><strong>Test behaviour</strong><p>Cover bug fixes and business-rule changes.</p></div>
</div>

## <strong>02</strong> C# and .NET conventions

- Target .NET 8 unless an upgrade is approved.
- Use nullable reference types and resolve warnings intentionally.
- Use async I/O and propagate cancellation tokens where supported.
- Keep controllers thin; place reusable rules in services.
- Use DTOs for API contracts rather than exposing EF entities.
- Use `decimal` for money.
- Avoid blocking calls such as `.Result` and `.Wait()` in request paths.
- Use dependency injection for infrastructure dependencies.

## <strong>03</strong> Naming conventions

| Item | Convention | Example |
|---|---|---|
| Class / public member | PascalCase | `OrdersController` |
| Interface | I + PascalCase | `IEmailService` |
| Method | PascalCase | `CreateShipmentAsync` |
| Local / parameter | camelCase | `orderId` |
| Constant | PascalCase | `MaximumUploadSize` |
| CSS class | kebab-case | `.product-card__price` |
| Test | Behaviour-focused | `CreateOrder_WhenPaymentFails_DoesNotReduceStock` |

## <strong>04</strong> API and frontend standards

<div class="doc-grid two">
<div class="doc-card purple"><span class="ico">🔌</span><strong>API contracts</strong><p>Correct HTTP methods, status codes, validation and non-sensitive errors.</p></div>
<div class="doc-card pink"><span class="ico">🧩</span><strong>Razor and UI</strong><p>Semantic HTML, reusable partials, accessible labels and responsive CSS.</p></div>
<div class="doc-card blue"><span class="ico">⚡</span><strong>JavaScript</strong><p>Modular behaviour, no duplicate handlers and explicit loading states.</p></div>
<div class="doc-card green"><span class="ico">🛡️</span><strong>Error handling</strong><p>Handle expected failures and log safely at the right boundary.</p></div>
</div>

## <strong>05</strong> Git contribution workflow

```mermaid
%%{init: {"theme":"base","themeVariables":{"background":"#fbf8fc","primaryColor":"#f7eaf3","primaryTextColor":"#302638","primaryBorderColor":"#b34e91","lineColor":"#8b3a62","secondaryColor":"#eff7ff","tertiaryColor":"#fff8e9","mainBkg":"#f7eaf3","nodeBorder":"#b34e91","clusterBkg":"#f5f0ff","clusterBorder":"#c9b5df","titleColor":"#56264e","edgeLabelBackground":"#fbf8fc","fontFamily":"Segoe UI, Arial, sans-serif"}}}%%
flowchart LR
 A[Update Base Branch] --> B[Create Focused Branch]
 B --> C[Implement Change]
 C --> D[Add Tests]
 D --> E[Build and Review]
 E --> F[Pull Request]
 F --> G[Merge After Approval]
```

1. Start from an up-to-date base branch.
2. Use a short branch name describing the work.
3. Make small commits with imperative messages.
4. Run relevant build and test commands.
5. Review the diff for secrets, generated files and unrelated changes.
6. Include purpose, test evidence and UI screenshots in pull requests.

## <strong>06</strong> Definition of done

- [ ] Requirement and existing behaviour inspected.
- [ ] Code follows project conventions.
- [ ] Validation and authorization remain server-side.
- [ ] Failure and retry paths considered.
- [ ] Tests added or updated.
- [ ] Build and tests pass.
- [ ] UI checked on mobile and desktop where relevant.
- [ ] Documentation and configuration updated.
- [ ] No secrets or personal data introduced.

<div class="doc-footer">UBHAYA FASHIONS · Engineering standards · Prefer small, reviewable and testable changes.</div>
