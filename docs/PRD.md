<div class="doc-hero">
<span class="doc-kicker">UBHAYA FASHIONS · PRODUCT STRATEGY</span><span class="doc-pill">Define what we build</span>
<h1>Product Requirements</h1>
<p>A shared product blueprint for building a trusted, elegant and easy-to-use women's fashion shopping experience.</p>
<div class="doc-meta"><div><strong>Product</strong>UBHAYA FASHIONS</div><div><strong>Platform</strong>ASP.NET Core 8 · Web</div><div><strong>Market</strong>India · INR</div></div>
</div>

## <strong>01</strong> Product vision

Build a polished fashion storefront where customers can discover clothing, understand product options, pay securely and follow their orders with confidence. Give store operators reliable tools to manage the catalogue, inventory, orders and fulfilment.

<div class="doc-grid">
<div class="doc-card pink"><span class="ico">🛍️</span><strong>Effortless discovery</strong><p>Beautiful product imagery, useful search, filters and curated collections.</p></div>
<div class="doc-card purple"><span class="ico">💎</span><strong>Premium experience</strong><p>Consistent visual design, clear product details and frictionless shopping.</p></div>
<div class="doc-card blue"><span class="ico">🔐</span><strong>Trusted checkout</strong><p>Server-verified payments, protected accounts and reliable order state.</p></div>
</div>

## <strong>02</strong> Product overview

| Attribute | Definition |
|---|---|
| Product | UBHAYA FASHIONS |
| Business | Women's fashion e-commerce |
| Frontend | ASP.NET Core MVC with Razor |
| Backend | ASP.NET Core Web API |
| Database | SQL Server / Entity Framework Core |
| Payments | Razorpay |
| Shipping | Delhivery B2C |
| Currency | INR (₹) |

## <strong>03</strong> Users and their needs

<div class="doc-grid two">
<div class="doc-card pink"><span class="ico">👩</span><strong>Guest shopper</strong><p>Browse products, compare variants, search and understand delivery information without creating an account.</p></div>
<div class="doc-card purple"><span class="ico">🧑‍💻</span><strong>Registered customer</strong><p>Manage profile and addresses, wishlist, cart, purchases and shipment tracking.</p></div>
<div class="doc-card blue"><span class="ico">🧰</span><strong>Store administrator</strong><p>Manage products, categories, variants, images, banners and inventory.</p></div>
<div class="doc-card green"><span class="ico">📦</span><strong>Order operator</strong><p>Review orders and payments, create eligible shipments and resolve fulfilment exceptions.</p></div>
</div>

## <strong>04</strong> Functional capabilities

| Capability | Expected behaviour |
|---|---|
| Storefront | Home page, collections, categories, responsive navigation |
| Catalogue | Search, filters, sorting and product detail |
| Variants | Size, colour, price, discount and stock selection |
| Identity | Email verification, sign-in, reset password and optional social login |
| Cart | Variant-aware cart with server-side stock validation |
| Checkout | Delivery address and trusted order summary |
| Payment | Razorpay order creation and backend signature verification |
| Orders | Customer history, admin management and status transitions |
| Fulfilment | Delhivery serviceability, manifestation, AWB and tracking |
| Admin | Product, inventory, customer, order and banner management |

## <strong>05</strong> Quality goals

<div class="doc-grid">
<div class="doc-card blue"><span class="ico">⚡</span><strong>Performance</strong><p>Optimized images, fast page rendering and efficient API/database calls.</p></div>
<div class="doc-card green"><span class="ico">♿</span><strong>Accessibility</strong><p>Keyboard support, labels, contrast, focus and reduced-motion support.</p></div>
<div class="doc-card yellow"><span class="ico">🛡️</span><strong>Security</strong><p>Least privilege, protected secrets and server-side validation.</p></div>
<div class="doc-card purple"><span class="ico">📱</span><strong>Responsive UX</strong><p>Reliable shopping experience from small phones to desktop screens.</p></div>
<div class="doc-card pink"><span class="ico">🔄</span><strong>Reliability</strong><p>Idempotent payment/shipping workflows and recoverable failures.</p></div>
<div class="doc-card orange"><span class="ico">🧩</span><strong>Maintainability</strong><p>Clear boundaries, automated tests and current documentation.</p></div>
</div>

## <strong>06</strong> Business rules

- The frontend must not connect directly to SQL Server.
- Backend calculations are authoritative for price, stock, payment and order state.
- Customers can access only their own private resources.
- Admin APIs require admin authorization.
- A prepaid order must not be manifested before payment verification.
- A shipment is successful only after a successful carrier response and saved AWB.
- Production secrets and customer data must never be committed to Git.

<div class="doc-callout"><span>💡</span><div><b>Product principle</b><br/>Make the shopping journey feel simple, while keeping the complex business rules safely enforced behind the API.</div></div>

## <strong>07</strong> Success measures

Track real measurements rather than assumed targets: product engagement, add-to-cart rate, checkout completion, payment success, fulfilment time, support contacts and mobile Core Web Vitals.

## <strong>08</strong> Acceptance checklist

- [ ] Guests can discover products and understand variants.
- [ ] Customers can complete a verified payment and view the order.
- [ ] Customer ownership and admin authorization are enforced.
- [ ] Stock cannot be oversold using stale browser values.
- [ ] Shipping failures are visible and safely recoverable.
- [ ] Critical journeys work on mobile and desktop.
- [ ] Automated tests and release checks pass.

<div class="doc-footer">UBHAYA FASHIONS · Product documentation · Update this document when product scope or business rules change.</div>
