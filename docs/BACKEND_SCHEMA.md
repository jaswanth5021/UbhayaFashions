# Backend schema

The EF Core source of truth is `backend/Data/ApplicationDbContext.cs`, the model configuration, and the migrations in `backend/Migrations/`. These simplified diagrams are split by domain so their entity names and relationships remain readable. They omit most columns, indexes, and some standalone support tables.

## Core relationship diagram

### Accounts

```mermaid
%%{init: {"theme":"base","themeVariables":{"background":"#fbf8fc","primaryColor":"#fff7f5","primaryTextColor":"#302522","primaryBorderColor":"#68142e","lineColor":"#68142e","textColor":"#302522","fontSize":"16px"},"er":{"theme":"base","look":"classic","layoutDirection":"LR","fill":"#fff7f5","stroke":"#68142e","fontSize":16,"entityPadding":10,"minEntityWidth":145,"minEntityHeight":58,"nodeSpacing":28,"rankSpacing":36,"diagramPadding":12}}}%%
erDiagram
    CUSTOMER ||--o{ EXTERNAL_LOGIN : links
    CUSTOMER ||--o{ CUSTOMER_ADDRESS : saves
    CUSTOMER {
        int Id PK
        string Email
        bool EmailVerified
    }
    EXTERNAL_LOGIN {
        int CustomerId FK
        string Provider
    }
    CUSTOMER_ADDRESS {
        int CustomerId FK
        string PostalCode
        bool IsDefault
    }
```

### Catalog and inventory

```mermaid
%%{init: {"theme":"base","themeVariables":{"background":"#fbf8fc","primaryColor":"#fff7f5","primaryTextColor":"#302522","primaryBorderColor":"#68142e","lineColor":"#68142e","textColor":"#302522","fontSize":"16px"},"er":{"theme":"base","look":"classic","layoutDirection":"LR","fill":"#fff7f5","stroke":"#68142e","fontSize":16,"entityPadding":10,"minEntityWidth":145,"minEntityHeight":58,"nodeSpacing":28,"rankSpacing":36,"diagramPadding":12}}}%%
erDiagram
    CATEGORY ||--o{ PRODUCT : groups
    PRODUCT ||--o{ PRODUCT_VARIANT : has
    PRODUCT ||--o{ PRODUCT_IMAGE : has
    PRODUCT ||--o{ PRODUCT_VIDEO : has
    PRODUCT ||--o{ INVENTORY_TRANSACTION : records
    PRODUCT {
        int Id PK
        int CategoryId FK
        string Name
    }
    PRODUCT_VARIANT {
        int Id PK
        int ProductId FK
        string Color
        string Size
        decimal Price
        int Stock
    }
    CATEGORY {
        int Id PK
        string Name
    }
```

### Orders and payments

```mermaid
%%{init: {"theme":"base","themeVariables":{"background":"#fbf8fc","primaryColor":"#fff7f5","primaryTextColor":"#302522","primaryBorderColor":"#68142e","lineColor":"#68142e","textColor":"#302522","fontSize":"16px"},"er":{"theme":"base","look":"classic","layoutDirection":"LR","fill":"#fff7f5","stroke":"#68142e","fontSize":16,"entityPadding":10,"minEntityWidth":145,"minEntityHeight":58,"nodeSpacing":28,"rankSpacing":36,"diagramPadding":12}}}%%
erDiagram
    CUSTOMER ||--o{ ORDER : places
    ORDER ||--|{ ORDER_ITEM : contains
    ORDER ||--o{ PAYMENT : has
    ORDER ||--o| DELHIVERY_SHIPMENT : may_have
    ORDER {
        int Id PK
        int CustomerId FK
        decimal TotalAmount
        string Status
        string PaymentStatus
    }
    ORDER_ITEM {
        int OrderId FK
        int ProductId FK
        int Quantity
    }
    PAYMENT {
        int Id PK
        int OrderId FK
        string RazorpayOrderId
        string Status
    }
    DELHIVERY_SHIPMENT {
        int OrderId FK
        string Waybill
        string ShipmentStatus
    }
```

### Customer activity

```mermaid
%%{init: {"theme":"base","themeVariables":{"background":"#fbf8fc","primaryColor":"#fff7f5","primaryTextColor":"#302522","primaryBorderColor":"#68142e","lineColor":"#68142e","textColor":"#302522","fontSize":"16px"},"er":{"theme":"base","look":"classic","layoutDirection":"LR","fill":"#fff7f5","stroke":"#68142e","fontSize":16,"entityPadding":10,"minEntityWidth":145,"minEntityHeight":58,"nodeSpacing":28,"rankSpacing":36,"diagramPadding":12}}}%%
erDiagram
    CUSTOMER ||--o{ WISHLIST_ITEM : owns
    CUSTOMER ||--o{ RECENTLY_VIEWED_PRODUCT : views
    CUSTOMER ||--o{ PRODUCT_REVIEW : writes
    CUSTOMER ||--o{ PRODUCT_REVIEW_VOTE : votes
    PRODUCT ||--o{ WISHLIST_ITEM : appears_in
    PRODUCT ||--o{ RECENTLY_VIEWED_PRODUCT : appears_in
    PRODUCT ||--o{ PRODUCT_REVIEW : receives
    PRODUCT_REVIEW ||--o{ PRODUCT_REVIEW_IMAGE : includes
    PRODUCT_REVIEW ||--o{ PRODUCT_REVIEW_VOTE : receives
```

### Notifications

```mermaid
%%{init: {"theme":"base","themeVariables":{"background":"#fbf8fc","primaryColor":"#fff7f5","primaryTextColor":"#302522","primaryBorderColor":"#68142e","lineColor":"#68142e","textColor":"#302522","fontSize":"16px"},"er":{"theme":"base","look":"classic","layoutDirection":"LR","fill":"#fff7f5","stroke":"#68142e","fontSize":16,"entityPadding":10,"minEntityWidth":145,"minEntityHeight":58,"nodeSpacing":28,"rankSpacing":36,"diagramPadding":12}}}%%
erDiagram
    STORE_NOTIFICATION ||--o{ CUSTOMER_NOTIFICATION_STATE : has_state
    CUSTOMER ||--o{ CUSTOMER_NOTIFICATION_STATE : tracks
```

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
