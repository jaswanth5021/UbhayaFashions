<div class="doc-hero"><span class="doc-kicker">UBHAYA FASHIONS · PAYMENT OPERATIONS</span><span class="doc-pill">Production integration guide</span><h1>Razorpay Webhook Setup</h1><p>Configure, validate and monitor server-to-server payment events for reliable order confirmation and inventory updates.</p><div class="doc-meta"><div><strong>Provider</strong>Razorpay</div><div><strong>Events</strong>Captured · Failed</div><div><strong>Secret</strong>Backend only</div></div></div>

## <strong>01</strong> Production endpoint

`POST https://ubhayafashions.in/api/payment/razorpay/webhook`

The endpoint accepts POST requests from Razorpay and validates the `X-Razorpay-Signature` header against the exact raw request body.

## <strong>02</strong> Configure the webhook secret

| Setting | Value |
|---|---|
| Environment variable | `Razorpay__WebhookSecret` |
| Configuration key | `Razorpay:WebhookSecret` |
| Storage | Backend hosting environment / secret store |

Use a strong unique secret. Do not reuse `Razorpay:KeySecret`, commit the secret or expose it to the frontend. Restart the backend application after changing IIS/Windows environment variables.

## <strong>03</strong> Razorpay dashboard setup

<div class="doc-grid two"><div class="doc-card pink"><span class="ico">🌐</span><strong>Webhook URL</strong><p>Use the production endpoint above. It accepts signed server-to-server POST requests.</p></div><div class="doc-card purple"><span class="ico">🔐</span><strong>Webhook secret</strong><p>Enter the exact same value configured as Razorpay__WebhookSecret on the backend.</p></div></div>

Subscribe to `payment.captured` and `payment.failed`, save the webhook and review delivery logs after testing.

## <strong>04</strong> What the implementation does

<div class="doc-grid two"><div class="doc-card green"><span class="ico">✓</span><strong>Signature validation</strong><p>Verifies HMAC-SHA256 against the raw body.</p></div><div class="doc-card blue"><span class="ico">🔁</span><strong>Duplicate protection</strong><p>Stores event IDs with a unique index and avoids repeat processing.</p></div><div class="doc-card purple"><span class="ico">💳</span><strong>Payment reconciliation</strong><p>Checks payment ID, order ID, amount, currency and captured status.</p></div><div class="doc-card pink"><span class="ico">📦</span><strong>Order fulfilment</strong><p>Commits paid state and stock changes before follow-up actions.</p></div></div>

If inventory is unavailable after a captured payment, the event is recorded as `NeedsReview` for admin intervention. It must not silently deduct negative stock.

## <strong>05</strong> Deployment and verification checklist

| Step | Action | Expected result |
|---|---|---|
| 1 | Deploy backend changes and apply migration | Required columns/table exist |
| 2 | Configure webhook secret | Backend loads secret |
| 3 | Create dashboard webhook | Endpoint and events saved |
| 4 | Send provider test event | Delivery and signature verified |
| 5 | Complete test-mode payment | Order and stock updated once |
| 6 | Replay same event | Duplicate safely ignored |
| 7 | Send invalid signature | HTTP 401 |
| 8 | Test failed payment | Order not marked paid |
| 9 | Configure live webhook after test approval | Live delivery verified |

## <strong>06</strong> Operational notes

- The authenticated `POST /api/payments/verify` checkout verification remains in place; webhook processing supplements it.
- Do not test by opening the URL in a browser address bar. That sends GET, not a signed POST.
- Never place the webhook secret in frontend settings, source control or customer-visible output.
- Monitor backend logs and Razorpay webhook delivery history.
- Confirm the current deployed route and migration state before production changes.

<div class="doc-callout"><span>⚠️</span><div><b>Keep environments separate</b><br/>Use separate test and live credentials. Verify webhook delivery and order behaviour in test mode before enabling live events.</div></div>
<div class="doc-footer">UBHAYA FASHIONS · Razorpay operations · Keep test and live credentials separate.</div>