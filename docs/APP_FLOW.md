<div align="center">

# Application Flow
### The customer journey and store operations

**A map of the key screens, transitions and system responsibilities.**

</div>

---

## 01 · Customer journey

```mermaid
flowchart TD
    A[Home] --> B[Browse / Search]
    B --> C[Filter and Sort]
    C --> D[Product Details]
    D --> E[Select Size and Colour]
    E --> F[Add to Cart]
    F --> G{Signed in?}
    G -- No --> H[Sign in / Register]
    G -- Yes --> I[Checkout]
    H --> I
    I --> J[Delivery Address]
    J --> K[Create Payment Order]
    K --> L[Razorpay Checkout]
    L --> M[Verify Payment on Server]
    M --> N{Verified?}
    N -- No --> O[Show Payment Pending / Failed]
    N -- Yes --> P[Persist Order and Update Stock]
    P --> Q[Confirmation]
    Q --> R[My Orders / Tracking]
```

### Journey details

| Step | Screen / action | Responsibility |
|---|---|---|
| Discover | Home, category, product listing | Render catalogue and navigation |
| Evaluate | Product detail | Show product media, variant options, stock and price |
| Select | Size, colour, quantity | Validate selected variant |
| Cart | Cart drawer/page | Re-check item availability and quantity |
| Checkout | Address and order summary | Validate address and authoritative totals |
| Pay | Razorpay Checkout | Complete provider payment interaction |
| Verify | Backend verification | Verify signature/provider state; protect against duplicate callbacks |
| Confirm | Order confirmation | Show only persisted order state |
| Aftercare | My Orders and tracking | Enforce customer ownership and show saved carrier status |

## 02 · Administrator journey

```mermaid
flowchart TD
    A[Admin Login] --> B[Admin Dashboard]
    B --> C[Catalogue]
    B --> D[Inventory]
    B --> E[Orders]
    B --> F[Customers]
    B --> G[Storefront Banners]
    C --> H[Product / Variant Editor]
    H --> I[Save and Validate]
    E --> J[Review Payment and Address]
    J --> K{Eligible to ship?}
    K -- No --> L[Resolve payment / order issue]
    K -- Yes --> M[Check PIN Serviceability]
    M --> N[Manifest with Delhivery]
    N --> O{AWB returned?}
    O -- Yes --> P[Persist Shipment and Tracking]
    O -- No --> Q[Show provider error; investigate safely]
```

## 03 · Payment state flow

```mermaid
stateDiagram-v2
    [*] --> Created
    Created --> PaymentPending
    PaymentPending --> Paid: server verification succeeds
    PaymentPending --> Failed: verified failure
    PaymentPending --> Pending: provider outcome unclear
    Pending --> Paid: reconciliation confirms
    Pending --> Failed: reconciliation confirms failure
    Paid --> Processing
    Processing --> Shipped
    Shipped --> Delivered
    Paid --> RefundPending: approved refund workflow
    RefundPending --> Refunded: provider confirms refund
```

> [!IMPORTANT]
> Treat this as a conceptual state model. Use the actual enum/status values and transition rules implemented in the current code before changing production behaviour.

## 04 · Shipment flow

```mermaid
sequenceDiagram
    participant Admin
    participant Web as Admin Website
    participant API as UBHAYA API
    participant Carrier as Delhivery
    participant DB as SQL Server
    Admin->>Web: Request shipment
    Web->>API: Authenticated shipment request
    API->>DB: Load order and verify eligibility
    API->>Carrier: Check serviceability
    Carrier-->>API: Serviceability result
    API->>Carrier: Manifest shipment
    Carrier-->>API: Result and AWB
    API->>DB: Persist shipment only on success
    API-->>Web: Shipment status
    Web-->>Admin: Confirmation or actionable error
```

## 05 · Navigation map

| Area | Typical route |
|---|---|
| Store home | `/` |
| Product listing | `/Products` |
| Admin sign-in | `/Admin/Login` |
| Admin area | `/Admin` |
| Customer account | `/Account` |
| Customer orders | `/Orders` |
| Order tracking | `/Orders/Track/{id}` |

Routes can evolve; check the relevant MVC controller and route attributes before relying on a path.

## 06 · UX states every journey needs

- Initial loading and skeleton state where appropriate.
- Empty state with a useful next action.
- Inline validation close to the relevant field.
- Success feedback after server confirmation.
- Failure feedback that explains the next safe action.
- Retry behaviour that does not duplicate payments, orders or shipments.
- Mobile-friendly focus, keyboard navigation and touch targets.
