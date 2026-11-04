# Razorpay → Delhivery → Customer AWB Email

## Implemented flow

1. Customer checks out and the frontend carries the structured delivery address (recipient name, phone, address lines, city, state, PIN code and country) into the payment verification request.
2. Razorpay payment signature is validated by the backend.
3. The backend independently retrieves the Razorpay payment and verifies captured status, Razorpay order ID and amount.
4. The backend commits the payment as Paid and deducts inventory.
5. After the payment transaction commits, the backend checks Delhivery configuration and destination serviceability, then creates a Delhivery shipment using the existing Delhivery service.
6. The returned AWB and label URL are saved in DelhiveryShipments against the order.
7. The order confirmation email is sent. If an AWB exists, the email contains the AWB and Delhivery tracking link. If shipment creation failed or was skipped, the email says tracking details will follow.
8. A Delhivery failure does not reverse the captured payment. The shipment is marked Creation Failed when a shipment record was created; the admin can retry from the Orders page.

## Required backend configuration

Configure these values in the backend environment / secret store. Do not commit API tokens or SMTP passwords to appsettings or Git.

- `Razorpay:KeyId` and `Razorpay:KeySecret` — matching Razorpay test keys for test mode, or live keys for live mode.
- `Delhivery:ApiToken` — token for the selected Delhivery environment.
- `Delhivery:Environment` — `Staging` or `Production`.
- `Delhivery:PickupLocationName` — exact active pickup location name in Delhivery One.
- `Delhivery:ClientName` — exact client name configured in Delhivery One (the automatic workflow checks that it is present).
- Existing Delhivery seller details and SMTP settings must also be configured as required by the current application.
- `FrontendUrl` should be the public storefront base URL so product images in the confirmation email can be resolved.

The automatic shipment uses a default parcel weight of **1000 grams**. Change this only after confirming the actual packing weight and the value Delhivery expects.

## Test checklist

### Before placing an order

- Confirm Razorpay test keys are active and match the current Razorpay mode.
- Set Delhivery to Staging and use the corresponding staging token.
- Confirm the pickup location is active and the exact configured name matches Delhivery One.
- Confirm `Delhivery:ClientName` is configured.
- Confirm the destination PIN code is serviceable for prepaid delivery in the selected Delhivery environment.
- Confirm the SMTP/email provider can send the order confirmation email.
- Restart the backend after changing environment variables.

### Run one controlled test

1. Sign in to the storefront.
2. Add one in-stock product to the cart.
3. Select or enter a complete delivery address. Verify name, mobile, city, state and six-digit PIN code.
4. Continue to Razorpay and complete a test payment using Razorpay's test payment method.
5. Wait for the storefront to redirect to the order success page.
6. Open the admin Orders page and find the new order.
7. Confirm Payment Status is Paid.
8. Confirm the Delhivery shipment status is Shipment Created and an AWB is visible.
9. Open the customer's confirmation email and verify that the AWB and tracking link are present.
10. Open the tracking link and confirm Delhivery recognises the AWB.

### Expected failure behaviour

- If Razorpay payment is not captured or verification fails, the order must not be marked Paid and no shipment should be created.
- If Delhivery rejects the PIN code, pickup location, token or shipment payload, the order remains Paid. The email is still sent without an AWB, and the admin should inspect the shipment error and retry after correcting the issue.
- Do not repeatedly retry if Delhivery may have created a shipment but the API response timed out. First check Delhivery One using the order reference/AWB to avoid duplicate manifests.
- Do not use a real customer order for initial testing. Use a controlled test order and confirm whether the selected Delhivery environment actually manifests shipments.

## Important scope

This automation is connected to the Razorpay `/api/payments/verify` flow. The separate test-only `/api/payments/test-success` endpoint and COD order flow have not been changed to automatically create Delhivery shipments. Admin shipment creation/retry remains available through the existing Orders workflow.
