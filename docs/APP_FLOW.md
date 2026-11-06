# Application flow

This page describes the current storefront and backend journeys. The frontend renders pages and calls the API; the API owns account, catalog, order, stock, and payment decisions.

## Browse to order

```mermaid
%%{init: {"theme":"base","themeVariables":{"background":"#fbf8fc","primaryColor":"#fff7f5","primaryTextColor":"#302522","primaryBorderColor":"#68142e","lineColor":"#68142e","edgeLabelBackground":"#fbf8fc","actorBkg":"#fff7f5","actorBorder":"#68142e","actorTextColor":"#302522","actorLineColor":"#68142e","signalColor":"#68142e","signalTextColor":"#302522","labelBoxBkgColor":"#fff7f5","labelBoxBorderColor":"#68142e","labelTextColor":"#302522","loopTextColor":"#302522","noteBkgColor":"#fff8e9","noteBorderColor":"#c69a45","noteTextColor":"#302522","activationBkgColor":"#f8e9e9","activationBorderColor":"#68142e","sequenceNumberColor":"#302522"}}}%%
flowchart TD
    A[Home, search or product listing] --> B[Open product details]
    B --> C[Choose size, color and quantity]
    C --> D[Add to cart]
    D --> E[Open cart drawer]
    E --> F[Select or add delivery address]
    F --> G{Payment method}
    G -->|Prepaid| H[Create pending order and Razorpay order]
    H --> I[Complete Razorpay Checkout]
    I --> J[Verify signature and captured payment]
    J --> K[Validate final stock and commit paid order]
    K --> L[Attempt shipment creation and send confirmation]
    G -->|COD| M[Create COD order and reduce stock]
    M --> N[Send order confirmation]
    L --> O[Customer views order/tracking]
    N --> O
```

## Payment boundaries

- `POST /api/payments/create-order` recalculates price from stored product variants and saves a pending order before creating the Razorpay order.
- `POST /api/payments/verify` validates the Razorpay signature, checks the payment's captured status, amount, and order association, then finalizes the order and stock change.
- `POST /api/payment/razorpay/webhook` accepts signed Razorpay events and deduplicates event IDs.
- A captured payment with an inventory conflict requires manual review/refund handling. A carrier failure does not reverse a captured payment.
- The test-only `POST /api/payments/test-success` endpoint is separate and controlled by `PaymentTesting:Enabled`.

## Account journeys

```mermaid
%%{init: {"theme":"base","themeVariables":{"background":"#fbf8fc","primaryColor":"#fff7f5","primaryTextColor":"#302522","primaryBorderColor":"#68142e","lineColor":"#68142e","edgeLabelBackground":"#fbf8fc","actorBkg":"#fff7f5","actorBorder":"#68142e","actorTextColor":"#302522","actorLineColor":"#68142e","signalColor":"#68142e","signalTextColor":"#302522","labelBoxBkgColor":"#fff7f5","labelBoxBorderColor":"#68142e","labelTextColor":"#302522","loopTextColor":"#302522","noteBkgColor":"#fff8e9","noteBorderColor":"#c69a45","noteTextColor":"#302522","activationBkgColor":"#f8e9e9","activationBorderColor":"#68142e","sequenceNumberColor":"#302522"}}}%%
flowchart LR
    A[Sign up] --> B[Customer account created]
    B --> C[Sign in]
    C --> D[Profile and saved addresses]
    D --> E[Optional email verification]
    C --> F[Wishlist and order history]
    C --> G[Request password reset]
    G --> H[Single-use reset link by email]
```

Customers can sign in with the configured supported methods. Email verification is optional after the current storefront signup; a signed-in customer requests a six-digit code from the profile. See [email verification setup](../frontend/EMAIL_VERIFICATION_SETUP.md).

## Admin journey

Admin pages use a separate frontend cookie. Backend admin APIs require an Admin role. Account management is restricted to the fixed owner address `admin@ubhaya.com`. Provision the initial owner out of band; the application does not seed a default admin.
