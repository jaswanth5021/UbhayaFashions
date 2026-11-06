<div class="doc-hero">
<span class="doc-kicker">UBHAYA FASHIONS · EXPERIENCE DESIGN</span><span class="doc-pill">Map every customer journey</span>
<h1>Application Flow</h1>
<p>A visual guide to customer shopping, administrator operations, payment verification and order fulfilment.</p>
<div class="doc-meta"><div><strong>Primary users</strong>Customers & administrators</div><div><strong>Critical path</strong>Browse → Pay → Deliver</div><div><strong>Integrations</strong>Razorpay · Delhivery</div></div>
</div>

## <strong>01</strong> Customer journey

```mermaid
flowchart TD
 A[Home] --> B[Browse / Search]
 B --> C[Filter and Sort]
 C --> D[Product Details]
 D --> E[Choose Size and Colour]
 E --> F[Add to Cart]
 F --> G{Signed in?}
 G -- No --> H[Sign in / Register]
 G -- Yes --> I[Checkout]
 H --> I
 I --> J[Delivery Address]
 J --> K[Create Payment Order]
 K --> L[Razorpay Checkout]
 L --> M[Server Payment Verification]
 M --> N{Verified?}
 N -- No --> O[Pending / Failed State]
 N -- Yes --> P[Save Order and Update Stock]
 P --> Q[Confirmation]
 Q --> R[My Orders / Tracking]
```

<div class="doc-grid four">
<div class="doc-card pink"><span class="ico">🔎</span><strong>01 · Discover</strong><p>Home, category, search and product listing.</p></div>
<div class="doc-card purple"><span class="ico">👗</span><strong>02 · Evaluate</strong><p>Images, sizes, colours, price and availability.</p></div>
<div class="doc-card blue"><span class="ico">🛒</span><strong>03 · Checkout</strong><p>Cart, address validation and payment.</p></div>
<div class="doc-card green"><span class="ico">📦</span><strong>04 · Aftercare</strong><p>Confirmation, order history and tracking.</p></div>
</div>

## <strong>02</strong> Administrator journey

```mermaid
flowchart TD
 A[Admin Login] --> B[Dashboard]
 B --> C[Catalogue]
 B --> D[Inventory]
 B --> E[Orders]
 B --> F[Customers]
 B --> G[Storefront Banners]
 C --> H[Product and Variant Editor]
 H --> I[Validate and Save]
 E --> J[Review Order and Payment]
 J --> K{Eligible to Ship?}
 K -- No --> L[Resolve Order Issue]
 K -- Yes --> M[Check PIN Serviceability]
 M --> N[Delhivery Manifest]
 N --> O{AWB returned?}
 O -- Yes --> P[Save Shipment and Tracking]
 O -- No --> Q[Show Provider Error]
```

## <strong>03</strong> Payment lifecycle

| Stage | System action | Expected result |
|---|---|---|
| Create | Backend creates Razorpay order | Provider order ID saved |
| Pay | Customer completes checkout | Provider payment result |
| Verify | Backend validates signature/status | Trusted payment outcome |
| Commit | Persist payment and order state | Consistent database state |
| Notify | Confirmation workflow | Customer receives order update |
| Reconcile | Resolve uncertain provider outcome | No duplicate payment/order |

> [!IMPORTANT]
> A browser redirect is not proof of payment. Only server-side verification and trusted provider events can establish the paid state.

## <strong>04</strong> Shipment sequence

```mermaid
sequenceDiagram
 participant Admin
 participant Web as Admin Website
 participant API as UBHAYA API
 participant D as Delhivery
 participant DB as SQL Server
 Admin->>Web: Request shipment
 Web->>API: Authenticated request
 API->>DB: Check order and payment
 API->>D: Check serviceability
 D-->>API: Serviceability result
 API->>D: Manifest shipment
 D-->>API: AWB and result
 API->>DB: Save successful shipment
 API-->>Web: Outcome
 Web-->>Admin: Confirmation or actionable error
```

## <strong>05</strong> Key screens and routes

| Area | Typical route |
|---|---|
| Store home | `/` |
| Product listing | `/Products` |
| Admin login | `/Admin/Login` |
| Admin area | `/Admin` |
| Customer account | `/Account` |
| Customer orders | `/Orders` |
| Order tracking | `/Orders/Track/{id}` |

Confirm exact routes in the current controllers before relying on them.

## <strong>06</strong> UX states

<div class="doc-grid">
<div class="doc-card blue"><span class="ico">⏳</span><strong>Loading</strong><p>Show progress without blocking unrelated navigation.</p></div>
<div class="doc-card purple"><span class="ico">🗂️</span><strong>Empty</strong><p>Explain what is missing and provide a next action.</p></div>
<div class="doc-card green"><span class="ico">✓</span><strong>Success</strong><p>Show only after server confirmation.</p></div>
<div class="doc-card orange"><span class="ico">⚠️</span><strong>Error</strong><p>Explain the issue and safe recovery path.</p></div>
</div>

<div class="doc-footer">UBHAYA FASHIONS · Experience documentation · Keep journeys aligned with actual routes and state transitions.</div>
