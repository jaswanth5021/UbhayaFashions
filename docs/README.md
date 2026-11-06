<div class="doc-hero">
<span class="doc-kicker">UBHAYA FASHIONS · DOCUMENTATION CENTER</span><span class="doc-pill">Project handbook</span>
<h1>Build with clarity.</h1>
<p>Your visual guide to the product, application flows, architecture, design language, security and engineering practices behind UBHAYA FASHIONS.</p>
<div class="doc-meta"><div><strong>11 guides</strong>Product and engineering</div><div><strong>.NET 8</strong>MVC + Web API</div><div><strong>One source</strong>Versioned with Git</div></div>
</div>

## <strong>01</strong> Explore the handbook

<div class="doc-grid two">
<div class="doc-card pink"><span class="ico">📘</span><strong>[Product Requirements](PRD.md)</strong><p>Vision, users, capabilities, business rules and quality goals.</p></div>
<div class="doc-card purple"><span class="ico">🧭</span><strong>[Application Flow](APP_FLOW.md)</strong><p>Customer journey, admin workflow, payment and shipment sequence.</p></div>
<div class="doc-card blue"><span class="ico">🏗️</span><strong>[System Architecture](ARCHITECTURE.md)</strong><p>Project boundaries, data flow, integrations and configuration.</p></div>
<div class="doc-card green"><span class="ico">🗄️</span><strong>[Backend Data Model](BACKEND_SCHEMA.md)</strong><p>Domain areas, relationships, persistence and migrations.</p></div>
<div class="doc-card pink"><span class="ico">🎨</span><strong>[Design System](DESIGN_SYSTEM.md)</strong><p>Brand direction, colours, components and responsive design.</p></div>
<div class="doc-card yellow"><span class="ico">🛡️</span><strong>[Security Guide](SECURITY.md)</strong><p>Authentication, payment safety, uploads and operational safeguards.</p></div>
<div class="doc-card purple"><span class="ico">⌨️</span><strong>[Code Style](CODE_STYLE.md)</strong><p>Conventions for C#, APIs, Razor, CSS and Git contributions.</p></div>
<div class="doc-card blue"><span class="ico">🧪</span><strong>[Testing Guide](TESTING.md)</strong><p>Test strategy, critical scenarios and release acceptance.</p></div>
<div class="doc-card green"><span class="ico">🚀</span><strong>[Implementation Plan](IMPLEMENTATION_PLAN.md)</strong><p>Workstreams, phases, milestones and prioritization.</p></div>
<div class="doc-card orange"><span class="ico">🤖</span><strong>[Agent & Contributor Guide](AGENTS.md)</strong><p>Safe workflow and rules for developers and AI assistants.</p></div>
<div class="doc-card pink"><span class="ico">💳</span><strong>[Razorpay Webhook Setup](RAZORPAY_WEBHOOK_SETUP.md)</strong><p>Webhook configuration, signature checks and verification steps.</p></div>
</div>

## <strong>02</strong> View the full visual design in VS Code

The repository includes a custom Markdown preview theme.

<div class="doc-grid two">
<div class="doc-card purple"><span class="ico">1️⃣</span><strong>Open a document</strong><p>For example, open `docs/TESTING.md` in VS Code.</p></div>
<div class="doc-card blue"><span class="ico">2️⃣</span><strong>Open Preview</strong><p>Press Ctrl + Shift + V, or use the Preview button.</p></div>
</div>

The normal editor shows Markdown source. The Preview pane renders the designed document with coloured headings, cards, tables and callouts. Theme files live in `.vscode/markdown.css` and `.vscode/settings.json`.

## <strong>03</strong> Existing setup guides

| Guide | Purpose |
|---|---|
| [Main project README](../README.md) | Local setup, architecture overview and project map |
| [Admin setup](../ADMIN_SETUP.md) | Administrator setup |
| [Address checkout setup](../ADDRESS_CHECKOUT_SETUP.md) | Address and checkout workflow |
| [Razorpay setup](../RAZORPAY_SETUP.md) | Payment setup |
| [Razorpay and Delhivery automation testing](../RAZORPAY_DELHIVERY_AUTOMATION_TESTING.md) | Integration test procedures |
| [Delhivery setup](../DELHIVERY_SETUP.md) | Carrier integration and troubleshooting |

## <strong>04</strong> Keep documentation healthy

- Update the relevant guide when behaviour or configuration changes.
- Prefer verified implementation details over assumptions.
- Never add credentials, customer data or private provider payloads.
- Keep copyable examples safe and use placeholders for secrets.
- Record completion only when validation evidence exists.

<div class="doc-callout"><span>✨</span><div><b>One product. One shared understanding.</b><br/>Use this handbook as the common reference for developers, reviewers and AI coding assistants.</div></div>
<div class="doc-footer">UBHAYA FASHIONS · Documentation center · Versioned with the application.</div>