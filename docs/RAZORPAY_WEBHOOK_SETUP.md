# Razorpay Webhook Setup – UBHAYA FASHIONS

## Production endpoint

`POST https://ubhayafashions.in/api/payment/razorpay/webhook`

The endpoint is public because Razorpay calls it server-to-server. It accepts POST requests only and validates the `X-Razorpay-Signature` header against the exact raw request body.

## Backend secret

Create a webhook secret in the Razorpay Dashboard and configure it on the backend as:

- Environment variable: `Razorpay__WebhookSecret`
- Configuration key: `Razorpay:WebhookSecret`

Use a strong, unique secret. Do not reuse `Razorpay:KeySecret`, do not commit the webhook secret to Git, and do not expose it to the frontend.

After adding or changing the environment variable on IIS/Windows, restart the backend application pool/site so the process loads the new value.

## Razorpay Dashboard

In the Razorpay Dashboard, create a webhook with:

- URL: `https://ubhayafashions.in/api/payment/razorpay/webhook`
- Secret: the same value configured as `Razorpay__WebhookSecret`
- Events: `payment.captured`, `payment.failed`

Keep the webhook secret exactly the same in both places.

## What the implementation does

- Verifies the webhook HMAC-SHA256 signature using the raw request body.
- Requires the `x-razorpay-event-id` header and stores it in a SQL Server table with a unique index.
- Treats duplicate event deliveries as successful duplicates without processing them again.
- Checks the payment ID, Razorpay order ID, amount, currency and captured status against the local payment/order.
- Marks captured payments paid and deducts stock in a database transaction.
- Records failed payments without changing an already-paid order.
- Creates a Delhivery shipment and sends the order confirmation after a captured payment is committed.
- Saves the structured checkout delivery address with the local order so the webhook can finish the paid-order flow even if the customer closes the browser.

If inventory is no longer available after Razorpay has captured payment, the webhook records the event as `NeedsReview` and logs a critical error. This requires an admin refund/order review; it does not silently mark the order paid or deduct negative stock.

## Deployment and test checklist

1. Deploy the backend changes and allow EF Core migrations to run. The migration adds `DeliveryAddressJson` to `Orders` and creates `RazorpayWebhookEvents`.
2. Configure `Razorpay__WebhookSecret` on the production server and restart the backend.
3. Create the webhook in Razorpay Dashboard with the URL and events above.
4. Use Razorpay's webhook test/send option, if available in the account, to verify successful delivery and signature validation.
5. Complete a Razorpay test-mode payment and confirm the order is paid, stock is deducted once, and the webhook event is stored.
6. Replay the same event and confirm the response indicates a duplicate and stock is unchanged.
7. Send an invalid-signature request and confirm the endpoint returns HTTP 401.
8. Test a failed payment and confirm the order is not marked paid and no stock/shipment/confirmation email is created.
9. After all tests pass, repeat configuration and verification using the live Razorpay account's webhook settings.

## Important operational notes

- The existing authenticated `POST /api/payments/verify` checkout verification remains in place. The webhook supplements it; it does not replace it.
- Never test the endpoint by typing its URL into a browser. A browser address-bar request is GET; Razorpay sends POST with signature and event headers.
- Never place the webhook secret in frontend settings, source control, or a customer-visible response.
- Monitor backend logs and the Razorpay webhook delivery logs after launch.
