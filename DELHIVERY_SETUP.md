# Delhivery B2C integration

This branch adds a separate shipment table, admin shipment creation UI/API, and customer-only order tracking.

## Backend environment variables

Keep tokens out of source control. Configure these in the backend hosting environment:

- `Delhivery__BaseUrl`: staging `https://staging-express.delhivery.com/`; production `https://track.delhivery.com/` only after Delhivery activates the live account.
- `Delhivery__ApiToken`: the token for the selected environment.
- `Delhivery__PickupLocationName`: exact registered pickup-location name from Delhivery One.
- `Delhivery__SellerName`: seller name.
- `Delhivery__SellerAddress`: seller/pickup address.
- `Delhivery__SellerPin`: seller postal code.
- `Delhivery__SellerPhone`: seller contact phone.

The service defaults to staging if BaseUrl is absent. Staging and production credentials differ. Do not commit the token or any live secret.

## Apply the database migration

Back up the database, then from the repository root run:

```bash
dotnet ef database update --project backend --startup-project backend
```

The migration adds `Orders.PaymentMethod` and `DelhiveryShipments`. Existing orders default to COD for backwards compatibility; new Razorpay orders are explicitly marked Prepaid.

## Create shipments from the admin website

Open Admin → Orders, expand **Create shipment**, check the recipient fields and provide city, state, PIN code and packed parcel weight. The request is admin-authenticated and goes through the backend. Prepaid orders cannot be shipped until payment verification has marked them Paid. After a waybill is saved, duplicate creation is blocked.

## Customer tracking

Signed-in customers see **Track Order** in Account → Orders. The backend checks both the order ID and the authenticated customer's ID. It refreshes Delhivery status no more frequently than once every two minutes and falls back to the last saved status if the carrier API is temporarily unavailable.

## Account readiness / production validation

The code uses Delhivery's documented legacy B2C shipment creation endpoint `api/cmu/create.json` and tracking endpoint `api/v1/packages/json/`. Delhivery One's current client developer portal may specify account-specific API URLs or required fields. After KYC, verify the endpoint/request/response contract, registered pickup name, serviceability check, seller information, dimensions, shipping mode, and token against the logged-in B2C API portal before enabling live production. This repository change cannot complete KYC or register a pickup location for you.
