# Backend schema

The EF Core source of truth is `backend/Data/ApplicationDbContext.cs`, the model configuration, and the migrations in `backend/Migrations/`. These simplified diagrams are split by domain so their entity names and relationships remain readable. They omit most columns, indexes, and some standalone support tables.

## Core relationship diagram

The overview below groups the current EF Core entities by business domain. Relationships are labelled with their cardinality. Standalone records are shown without artificial links. The diagram uses a simple Mermaid flowchart for readability in GitHub and common Markdown previews.

```mermaid
flowchart TB
    subgraph ACCOUNTS["ACCOUNTS"]
        direction TB
        CUSTOMER["Customer"]
        ADDRESS["CustomerAddress"]
        LOGIN["ExternalLogin"]
        PENDING["PendingSignup"]
        ADMIN["AdminUser"]
        CUSTOMER -->|"1 : many"| ADDRESS
        CUSTOMER -->|"1 : many"| LOGIN
    end

    subgraph CATALOG["CATALOG AND INVENTORY"]
        direction TB
        CATEGORY["Category"]
        PRODUCT["Product"]
        VARIANT["ProductVariant"]
        IMAGE["ProductImage"]
        VIDEO["ProductVideo"]
        INVENTORY["InventoryTransaction"]
        CATEGORY -->|"1 : many"| PRODUCT
        PRODUCT -->|"1 : many"| VARIANT
        PRODUCT -->|"1 : many"| IMAGE
        PRODUCT -->|"1 : many"| VIDEO
        PRODUCT -->|"1 : many"| INVENTORY
    end

    subgraph COMMERCE["ORDERS AND PAYMENTS"]
        direction TB
        ORDER["Order"]
        ORDER_ITEM["OrderItem"]
        PAYMENT["Payment"]
        SHIPMENT["DelhiveryShipment"]
        WEBHOOK["RazorpayWebhookEvent"]
        ORDER -->|"1 : many"| ORDER_ITEM
        ORDER -->|"1 : many"| PAYMENT
        ORDER -->|"1 : zero-or-one"| SHIPMENT
    end

    subgraph CUSTOMER_ACTIVITY["CUSTOMER ACTIVITY"]
        direction TB
        WISHLIST["WishlistItem"]
        RECENT["RecentlyViewedProduct"]
        REVIEW["ProductReview"]
        REVIEW_IMAGE["ProductReviewImage"]
        REVIEW_VOTE["ProductReviewVote"]
        REVIEW -->|"1 : many"| REVIEW_IMAGE
        REVIEW -->|"1 : many"| REVIEW_VOTE
    end

    subgraph NOTIFICATIONS["NOTIFICATIONS"]
        direction TB
        NOTIFICATION["StoreNotification"]
        NOTIFICATION_STATE["CustomerNotificationState"]
        NOTIFICATION -->|"1 : many"| NOTIFICATION_STATE
    end

    CUSTOMER -->|"1 : many"| ORDER
    PRODUCT -->|"1 : many"| ORDER_ITEM
    CUSTOMER -->|"1 : many"| WISHLIST
    PRODUCT -->|"1 : many"| WISHLIST
    CUSTOMER -->|"1 : many"| RECENT
    PRODUCT -->|"1 : many"| RECENT
    CUSTOMER -->|"1 : many"| REVIEW
    PRODUCT -->|"1 : many"| REVIEW
    CUSTOMER -->|"1 : many"| REVIEW_VOTE
    CUSTOMER -->|"1 : many"| NOTIFICATION_STATE

    classDef account fill:#f7e9ed,stroke:#8b4055,color:#302522,stroke-width:1.5px;
    classDef catalog fill:#f8efe1,stroke:#ad7b36,color:#302522,stroke-width:1.5px;
    classDef commerce fill:#e9eff9,stroke:#5577a5,color:#202b3a,stroke-width:1.5px;
    classDef activity fill:#e9f3ed,stroke:#54866a,color:#24372b,stroke-width:1.5px;
    classDef notification fill:#f1ecf8,stroke:#8066a5,color:#302542,stroke-width:1.5px;

    class CUSTOMER,ADDRESS,LOGIN,PENDING,ADMIN account;
    class CATEGORY,PRODUCT,VARIANT,IMAGE,VIDEO,INVENTORY catalog;
    class ORDER,ORDER_ITEM,PAYMENT,SHIPMENT,WEBHOOK commerce;
    class WISHLIST,RECENT,REVIEW,REVIEW_IMAGE,REVIEW_VOTE activity;
    class NOTIFICATION,NOTIFICATION_STATE notification;
```

**Relationship notes**

- `PendingSignup`, `AdminUser`, and `RazorpayWebhookEvent` are independent records in the current EF Core model; no entity foreign-key relationship is drawn for them.
- `DelhiveryShipment` has a unique `OrderId`, so an order can have at most one shipment record.
- Customer activity records connect customers and products through their own entity rows.
- The diagram shows the principal foreign-key relationships, not every scalar property or index.

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
