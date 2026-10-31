# Delhivery B2C integration

This integration keeps the Delhivery API token on the backend. The frontend must call the authenticated UBHAYA backend; never call Delhivery directly from browser JavaScript.

## Backend environment variables

Keep tokens out of source control. Configure these in the backend hosting environment:

- `Delhivery__BaseUrl`: staging `https://staging-express.delhivery.com/`; production `https://track.delhivery.com/` only after Delhivery activates the live account.
- `Delhivery__ApiToken`: the token for the selected environment.
- `Delhivery__ClientName`: exact, case-sensitive client name shown in the Delhivery B2C API account/manifestation portal. Do not guess this value.
- `Delhivery__PickupLocationName`: exact registered and active pickup-location name from Delhivery One.
- `Delhivery__SellerName`: seller name.
- `Delhivery__SellerAddress`: seller/pickup address.
- `Delhivery__SellerPin`: seller postal code.
- `Delhivery__SellerPhone`: seller contact phone.
- `Delhivery__LabelBaseUrl` (optional): override the base URL for the packing-slip/label endpoint if Delhivery supplies a separate host for the account/environment.

The service defaults to staging if BaseUrl is absent. Staging and production credentials differ. Do not commit the token or any live secret.

## Admin API endpoints

All endpoints below require an Admin JWT:

- `GET /api/admin/delhivery/serviceability/{postalCode}` — checks prepaid, COD and pickup serviceability.
- `GET /api/admin/delhivery/shipping-cost?originPin=500084&destinationPin=500084&weightGrams=1000&shipmentMode=Surface&paymentMode=Pre-paid&status=Delivered` — returns Delhivery's approximate rate response. The API allows 40 requests/minute. Staging can return zero charges because rates are not stored there.
- `GET /api/admin/delhivery/waybills?count=1` — optionally pre-fetches waybills. For normal shipment creation, the integration leaves the waybill blank so Delhivery can generate it in the manifestation response.
- `POST /api/admin/orders/{orderId}/delhivery-shipment` — checks serviceability first, then manifests the order. Prepaid orders are only sent after payment is verified.
- `GET /api/admin/delhivery/label/{waybill}` — downloads a PDF packing slip for a manifested shipment.
- Customer tracking remains available through `GET /api/orders/{id}/tracking`.

## Create shipments from the admin website

Open Admin → Orders, expand **Create shipment**, check the recipient fields and provide city, state, PIN code and packed parcel weight. The request is admin-authenticated and goes through the backend. Prepaid orders cannot be shipped until payment verification has marked them Paid. A non-serviceable PIN is rejected before the manifestation request is sent, avoiding the partial-save error seen in staging. After a waybill is saved, duplicate creation is blocked.

Delhivery's FAQ confirms that the waybill can be left blank in the shipment creation request to let Delhivery generate it, and that the exact client name is case-sensitive. The shipment is only considered created when the API returns a successful package and a waybill.

## Rate calculation

Delhivery's invoice/rate endpoint returns an estimate only. The rate calculation uses billing mode `E` (Express) or `S` (Surface), chargeable weight in grams, origin and destination PIN codes, shipment status (`Delivered`, `RTO`, or `DTO`), and payment type (`Pre-paid` or `COD`). Staging may return zero charges; use the approved production token/environment for current rates.

## Customer tracking and labels

Signed-in customers see **Track Order** in Account → Orders. The backend checks both the order ID and the authenticated customer's ID. It refreshes Delhivery status no more frequently than once every two minutes and falls back to the last saved status if the carrier API is temporarily unavailable. Admins can download a PDF label through the label endpoint after a shipment has been manifested.

## Apply the database migration

The existing migration adds `Orders.PaymentMethod` and `DelhiveryShipments`. No new database columns are required for the API additions in this change.

## Production readiness

The code uses Delhivery's B2C pincode serviceability, bulk waybill, shipment manifestation, tracking, invoice/rate, and packing-slip endpoints. Before switching to production, verify the exact case-sensitive client name, pickup location, seller details, production token, production base URL, label host, prepaid/COD enablement, and account billing requirements with Delhivery. Keep staging and production credentials separate.
