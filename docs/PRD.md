<div align="center">

# Product Requirements Document
### UBHAYA FASHIONS · Online fashion storefront

**The product definition — audience, outcomes, capabilities and quality expectations.**

</div>

---

## 01 · Product overview

| Field | Definition |
|---|---|
| Product | UBHAYA FASHIONS |
| Category | Women's fashion e-commerce |
| Primary experience | Responsive web storefront |
| Application shape | ASP.NET Core MVC frontend + ASP.NET Core Web API |
| Persistence | SQL Server through Entity Framework Core |
| Primary market | India |
| Primary currency | INR (₹) |
| Current payment provider | Razorpay |
| Shipping integration | Delhivery B2C |

### Product vision

Make discovering and purchasing women's clothing simple, trustworthy and visually engaging—from product discovery through secure payment, delivery updates and post-purchase support.

## 02 · Users and needs

| User | Needs |
|---|---|
| Guest shopper | Browse collections, search, filter and understand product details |
| Registered customer | Manage profile and addresses, wishlist, cart, orders and tracking |
| Store administrator | Maintain products, variants, media, inventory and storefront content |
| Order operator | Review payment/order state, create eligible shipments and track fulfilment |
| Store owner | Manage privileged admin access and monitor business operations |

## 03 · Product objectives

1. Make product discovery fast and intuitive on mobile and desktop.
2. Present clear product imagery, sizes, colours, pricing and availability.
3. Protect customer accounts and payment workflows.
4. Keep stock and order state consistent across checkout and fulfilment.
5. Give customers understandable order confirmation and tracking.
6. Give administrators safe tools to operate the store without direct database edits.

## 04 · Functional requirements

### Storefront and discovery

- Home page with curated collections, categories and promotional banners.
- Product listing with search, category, size, colour, price and availability filters.
- Product detail with images, available variants, stock, pricing and reviews where available.
- Responsive navigation and persistent access to account, wishlist and cart.
- Clear empty, loading, validation and error states.

### Identity and customer account

- Email-based registration and verification.
- Secure sign-in and sign-out.
- Optional social sign-in only when provider credentials are configured.
- Password reset with expiring, single-use tokens.
- Customer profile, saved addresses and account/order history.
- Authorization must be enforced server-side for customer-owned resources.

### Cart, checkout and payment

- Cart supports product variant, quantity and price display.
- Validate current product price and stock on the server at checkout.
- Checkout collects and validates delivery information.
- Razorpay order creation and server-side payment signature verification.
- Persist payment/order state idempotently; do not trust browser success alone.
- Reduce inventory only after the payment workflow has been verified.

### Orders and fulfilment

- Customer order history and order details.
- Admin order management and status transitions.
- Delhivery serviceability check and shipment manifestation for eligible orders.
- Save carrier AWB/tracking data only after successful provider response.
- Customer-facing tracking with ownership checks.
- Cancellation/refund behaviour must follow the configured business policy and current order state.

### Administration

- Product and category management.
- Product variants, pricing, discounts, stock, colours and media.
- Inventory adjustments with auditable outcomes.
- Storefront banner management.
- Order, customer and payment visibility.
- Restricted admin-account management for the designated owner role.
- Operational error visibility without exposing secrets or sensitive customer data.

## 05 · Non-functional requirements

| Quality | Requirement |
|---|---|
| Performance | Fast first render, optimized images/assets, efficient API calls and database queries |
| Accessibility | Keyboard access, visible focus, labels, semantic structure and sufficient contrast |
| Responsive design | Usable layouts on small phones through desktop widths |
| Security | Least privilege, server-side validation, protected secrets and secure sessions |
| Reliability | Idempotent payment/shipping operations and recoverable provider failures |
| Maintainability | Clear project boundaries, tests and versioned documentation |
| Observability | Actionable logs with correlation context and sensitive-data redaction |
| SEO | Descriptive page titles, metadata, canonical URLs and crawlable product content |

## 06 · Business rules and invariants

- The website frontend does not connect directly to SQL Server.
- The backend is authoritative for price, stock, payment and order state.
- A customer can access only their own private account, address and order data.
- Admin APIs require admin authorization; a customer token is not an admin credential.
- Never manifest a prepaid order before payment is verified.
- A failed carrier response must not be represented as a successfully created shipment.
- Never commit live API keys, SMTP credentials, JWT signing keys or personal customer data.

## 07 · Out of scope unless separately approved

- Native Android/iOS applications.
- Multi-vendor marketplace workflows.
- Automatic AI-generated product descriptions without human review.
- New payment or courier providers without an integration decision.
- Claims of certifications or compliance that have not been independently established.

## 08 · Success measures

Track these using reliable analytics and operational data; do not invent baseline values.

- Product-list-to-product-detail engagement.
- Add-to-cart and checkout completion rates.
- Payment success and reconciliation rates.
- Order fulfilment time and carrier exception rate.
- Mobile Core Web Vitals and page performance.
- Support contacts caused by checkout, payment or tracking confusion.

## 09 · Acceptance checklist

- [ ] A guest can discover products and understand available variants.
- [ ] A customer can complete a verified payment and view the resulting order.
- [ ] Customers cannot access another customer's private resources.
- [ ] Admin operations are protected by role checks.
- [ ] Stock cannot be oversold by trusting stale browser data.
- [ ] Shipping failures are visible and safely retryable.
- [ ] Key journeys work at mobile and desktop widths.
- [ ] Tests and release checks pass.
