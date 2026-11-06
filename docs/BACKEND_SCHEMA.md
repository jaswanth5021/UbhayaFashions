# Backend schema

The EF Core source of truth is `backend/Data/ApplicationDbContext.cs`, the model configuration, and the migrations in `backend/Migrations/`. These diagrams are simplified views of the current model. They focus on entity relationships and omit most scalar columns and indexes.

## Core relationship overview

The schema is split into small diagrams so entity names, cardinalities, and relationship lines remain readable in GitHub and common Markdown previews. Each domain has its own colour. The final diagram shows the links that cross domain boundaries.

### 1. Accounts

```mermaid
flowchart TB
    C(["Customer"])
    A["CustomerAddress"]
    L["ExternalLogin"]
    P["PendingSignup"]
    AD["AdminUser"]

    C -->|"1 : many"| A
    C -->|"1 : many"| L

    classDef primary fill:#DCEBFF,stroke:#2563EB,color:#172554,stroke-width:2px;
    classDef child fill:#EFF6FF,stroke:#60A5FA,color:#1E3A8A;
    classDef standalone fill:#F3F4F6,stroke:#6B7280,color:#111827;
    class C primary;
    class A,L child;
    class P,AD standalone;
```

### 2. Catalog and inventory

```mermaid
flowchart TB
    CAT(["Category"])
    P(["Product"])
    V["ProductVariant"]
    I["ProductImage"]
    VID["ProductVideo"]
    T["InventoryTransaction"]

    CAT -->|"1 : many"| P
    P -->|"1 : many"| V
    P -->|"1 : many"| I
    P -->|"1 : many"| VID
    P -->|"1 : many"| T

    classDef primary fill:#D1FAE5,stroke:#059669,color:#064E3B,stroke-width:2px;
    classDef child fill:#ECFDF5,stroke:#34D399,color:#065F46;
    class CAT,P primary;
    class V,I,VID,T child;
```

### 3. Orders and payments

```mermaid
flowchart TB
    O(["Order"])
    OI["OrderItem"]
    PAY["Payment"]
    S["DelhiveryShipment"]
    W["RazorpayWebhookEvent"]

    O -->|"1 : many"| OI
    O -->|"1 : many"| PAY
    O -->|"1 : zero-or-one"| S

    classDef primary fill:#FFEDD5,stroke:#EA580C,color:#7C2D12,stroke-width:2px;
    classDef child fill:#FFF7ED,stroke:#FB923C,color:#9A3412;
    classDef standalone fill:#F3F4F6,stroke:#6B7280,color:#111827;
    class O primary;
    class OI,PAY,S child;
    class W standalone;
```

### 4. Customer activity

```mermaid
flowchart TB
    C(["Customer"])
    P(["Product"])
    W["WishlistItem"]
    R["RecentlyViewedProduct"]
    REV["ProductReview"]
    RI["ProductReviewImage"]
    RV["ProductReviewVote"]

    C -->|"1 : many"| W
    P -->|"1 : many"| W
    C -->|"1 : many"| R
    P -->|"1 : many"| R
    C -->|"1 : many"| REV
    P -->|"1 : many"| REV
    REV -->|"1 : many"| RI
    REV -->|"1 : many"| RV
    C -->|"1 : many"| RV

    classDef parent fill:#EDE9FE,stroke:#7C3AED,color:#4C1D95,stroke-width:2px;
    classDef activity fill:#F5F3FF,stroke:#A78BFA,color:#5B21B6;
    class C,P parent;
    class W,R,REV,RI,RV activity;
```

### 5. Notifications

```mermaid
flowchart TB
    N(["StoreNotification"])
    S["CustomerNotificationState"]
    P["Product (optional link)"]
    C["Customer"]

    N -->|"1 : many"| S
    C -->|"1 : many"| S
    P -.->|"optional product reference"| N

    classDef notification fill:#FCE7F3,stroke:#DB2777,color:#831843,stroke-width:2px;
    classDef related fill:#FDF2F8,stroke:#F472B6,color:#9D174D;
    N classDef notification;
    class S,P,C related;
```

### 6. Cross-domain relationships

This view contains only the main links between business areas. Refer to the domain diagrams above for each area's internal relationships.

```mermaid
flowchart TB
    C(["Customer"])
    P(["Product"])
    O(["Order"])
    OI["OrderItem"]
    W["WishlistItem"]
    R["RecentlyViewedProduct"]
    REV["ProductReview"]
    NS["CustomerNotificationState"]
    N["StoreNotification"]

    C -->|"places"| O
    O -->|"contains"| OI
    P -->|"appears in"| OI
    C -->|"saves"| W
    P -->|"saved as"| W
    C -->|"views"| R
    P -->|"viewed as"| R
    C -->|"writes"| REV
    P -->|"reviewed in"| REV
    C -->|"receives state for"| NS
    N -->|"has customer state"| NS
    P -.->|"optional link"| N

    classDef customer fill:#DCEBFF,stroke:#2563EB,color:#172554,stroke-width:2px;
    classDef product fill:#D1FAE5,stroke:#059669,color:#064E3B,stroke-width:2px;
    classDef order fill:#FFEDD5,stroke:#EA580C,color:#7C2D12,stroke-width:2px;
    classDef activity fill:#F5F3FF,stroke:#7C3AED,color:#4C1D95;
    classDef notice fill:#FCE7F3,stroke:#DB2777,color:#831843;
    class C customer;
    class P product;
    class O order;
    class OI,W,R,REV activity;
    class NS,N notice;
```

**Relationship notes**

- `PendingSignup`, `AdminUser`, and `RazorpayWebhookEvent` are independent records in the current EF Core model; no entity foreign-key relationship is drawn for them.
- `DelhiveryShipment` has a unique `OrderId`, so an order can have at most one shipment record.
- Customer activity records connect customers and products through their own entity rows.
- A store notification may optionally reference a product; deleting that product clears the notification link.
- The diagrams show principal foreign-key relationships, not every scalar property or index.

## Model groups

- **Accounts:** `Customers`, `PendingSignups`, `ExternalLogins`, `AdminUsers`, `CustomerAddresses`
- **Catalog:** `Categories`, `Products`, `ProductVariants`, `ProductImages`, `ProductVideos`
- **Commerce:** `Orders`, `OrderItems`, `Payments`, `RazorpayWebhookEvents`, `DelhiveryShipments`, `InventoryTransactions`
- **Customer activity:** `WishlistItems`, `RecentlyViewedProducts`, `ProductReviews`, `ProductReviewImages`, `ProductReviewVotes`
- **Notifications:** `StoreNotification` records (DbSet exposed as `Notifications`) and `CustomerNotificationStates`

## Important constraints

- Product variant identity is unique by `(ProductId, Color, Size)`.
- Razorpay order IDs and webhook event IDs are unique; a non-null Razorpay payment ID is unique.
- An order has at most one Delhivery shipment record.
- A customer can review a product once; review votes are unique per customer and review.
- Customer notification state uses the composite key `(CustomerId, NotificationId)`.
- Product categories are protected from deletion while referenced. Deleting a product clears its optional product link from a store notification.

Migrations are applied automatically at API startup. For a deployment that manages schema changes separately, use `dotnet ef database update --project backend/backend.csproj --startup-project backend/backend.csproj` and coordinate the operation with the database backup and rollback plan.
