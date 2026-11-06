<div align="center">

# UBHAYA FASHIONS
### Project documentation hub

**A practical guide to the product, architecture, design language, engineering standards and delivery process.**

![Documentation](https://img.shields.io/badge/docs-project%20handbook-8B3A62?style=for-the-badge)
![Platform](https://img.shields.io/badge/platform-ASP.NET%20Core%208-512BD4?style=for-the-badge)
![Database](https://img.shields.io/badge/database-SQL%20Server-CC2927?style=for-the-badge)

</div>

---

> [!NOTE]
> This handbook describes the repository as it exists on the `master` branch and records the intended product and engineering practices. Where a detail depends on deployment configuration or an external provider account, verify the live environment before acting.

## Documentation map

| Document | What it answers |
|---|---|
| [Product Requirements](PRD.md) | What UBHAYA is building and for whom |
| [App Flow](APP_FLOW.md) | How customers and administrators move through the product |
| [Architecture](ARCHITECTURE.md) | How the website, API and database work together |
| [Backend Schema](BACKEND_SCHEMA.md) | Data model, relationships and persistence rules |
| [Design System](DESIGN_SYSTEM.md) | Visual principles, components and responsive behaviour |
| [Security](SECURITY.md) | Trust boundaries, secrets and operational safeguards |
| [Code Style](CODE_STYLE.md) | Conventions for safe, consistent changes |
| [Testing](TESTING.md) | Validation strategy and test commands |
| [Implementation Plan](IMPLEMENTATION_PLAN.md) | Delivery phases, milestones and release gates |
| [AI Agent Guide](AGENTS.md) | Rules for AI coding agents and contributors |\n| [Razorpay Webhook Setup](RAZORPAY_WEBHOOK_SETUP.md) | Production webhook configuration and verification |

## View the designed documentation in VS Code\n\nThe repository includes a workspace Markdown preview theme in `.vscode/markdown.css`. To see the coloured layout, open any file under `docs/` and press **Ctrl+Shift+V** (Windows/Linux) or **Cmd+Shift+V** (macOS), or right-click and choose **Open Preview**. The normal editor tab shows Markdown source; the styled page appears in the preview pane.\n\n## Start here

1. Read the [Product Requirements](PRD.md) for product context.
2. Follow [Architecture](ARCHITECTURE.md) before changing cross-project behaviour.
3. Review [Security](SECURITY.md) before touching authentication, payments, uploads or shipping.
4. Use [Testing](TESTING.md) to validate the change before opening a pull request.

## Existing operational guides

These root-level guides remain the source for their specific setup procedures:

- [Main project README](../README.md)
- [Admin setup](../ADMIN_SETUP.md)
- [Address and checkout setup](../ADDRESS_CHECKOUT_SETUP.md)
- [Razorpay setup](../RAZORPAY_SETUP.md)
- [Razorpay and Delhivery automation testing](../RAZORPAY_DELHIVERY_AUTOMATION_TESTING.md)
- [Delhivery setup](../DELHIVERY_SETUP.md)

## Repository at a glance

| Area | Location | Responsibility |
|---|---|---|
| Storefront and admin UI | `frontend/` | ASP.NET Core MVC, Razor views, browser assets |
| JSON API | `backend/` | API endpoints, business rules, authentication |
| Persistence | `backend/Data/` | EF Core context and SQL Server access |
| Schema evolution | `backend/Migrations/` | EF Core migrations |
| Automated tests | `tests/` | Regression and integration coverage |
| Local orchestration | `docker-compose.yml` | SQL Server, API and web containers |

## Documentation maintenance

- Update the relevant document in the same change as a behaviour or architecture change.
- Prefer verified implementation details over assumptions.
- Never put credentials, customer data, production URLs containing secrets, or private provider payloads in documentation.
- Keep examples safe to copy; use placeholders for credentials.
- Add a dated entry to the implementation plan only when a milestone or decision actually changes.

<sub>Maintained with the UBHAYA FASHIONS codebase · Documentation is versioned with the application.</sub>
