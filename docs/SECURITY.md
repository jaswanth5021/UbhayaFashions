<div class="doc-hero">
<span class="doc-kicker">UBHAYA FASHIONS · TRUST & SAFETY</span><span class="doc-pill">Protect every transaction</span>
<h1>Security Guide</h1>
<p>Practical safeguards for customer identity, payments, shipping, uploaded files, production data and store operations.</p>
<div class="doc-meta"><div><strong>Security model</strong>Layered controls</div><div><strong>Trust boundary</strong>Browser → API</div><div><strong>Priority</strong>Production safety</div></div>
</div>

## <strong>01</strong> Trust boundaries

<figure class="doc-diagram">
  <img src="assets/security-trust-boundaries.svg" alt="Security trust boundaries between browser, website, backend and external providers." />
  <figcaption>Security trust boundaries between browser, website, backend and external providers.</figcaption>
</figure>

Validate every value crossing a boundary. Browser validation is for usability; backend validation is mandatory.

## <strong>02</strong> Security pillars

<div class="doc-grid">
<div class="doc-card pink"><span class="ico">🔑</span><strong>Secrets</strong><p>Keep credentials out of source control and browser code.</p></div>
<div class="doc-card purple"><span class="ico">🪪</span><strong>Identity</strong><p>Separate customer and admin authentication contexts.</p></div>
<div class="doc-card blue"><span class="ico">💳</span><strong>Payments</strong><p>Verify signatures and reconcile uncertain outcomes.</p></div>
<div class="doc-card green"><span class="ico">🚚</span><strong>Shipping</strong><p>Protect carrier tokens and shipment operations.</p></div>
<div class="doc-card yellow"><span class="ico">📁</span><strong>Uploads</strong><p>Validate file content and store outside executable paths.</p></div>
<div class="doc-card orange"><span class="ico">📋</span><strong>Observability</strong><p>Log safely without exposing personal or secret data.</p></div>
</div>

## <strong>03</strong> Authentication and authorization

- Use strong password hashing; never store plaintext passwords.
- Use secure, HTTP-only cookies and HTTPS in production.
- Validate JWT signature, issuer, audience and expiry.
- Enforce authorization on API endpoints.
- Check ownership for customer orders, addresses, reviews and account actions.
- Keep admin and customer credentials/claims separate.
- Use expiring, single-use password-reset tokens.

## <strong>04</strong> Payment protection

| Control | Requirement |
|---|---|
| API keys | Secret key stays on backend |
| Amount | Calculate from trusted server-side data |
| Signature | Verify on backend |
| Webhook | Validate signature and event identity |
| Duplicate event | Process idempotently |
| Timeout | Reconcile before retrying |
| Order state | Never trust browser redirect alone |

## <strong>05</strong> Shipping protection

- Keep Delhivery API token server-side.
- Require admin authorization for shipment and pickup operations.
- Check order state and payment eligibility before manifestation.
- Validate destination PIN, recipient details and parcel weight.
- Save AWB only after confirmed successful response.
- Prevent duplicate shipment creation.
- Redact personal details from normal logs.

## <strong>06</strong> Input, files and database

- Validate request DTOs on the server.
- Use parameterized EF Core queries.
- Validate file extension, MIME type, signature and size.
- Generate safe filenames and prevent path traversal.
- Store uploads outside executable application directories.
- Use least-privilege database credentials.
- Back up database and uploads separately; test restore.

## <strong>07</strong> Release security gate

<div class="doc-grid two">
<div class="doc-card green"><span class="ico">✓</span><strong>Identity and access</strong><p>Authorization, customer ownership and secure sessions tested.</p></div>
<div class="doc-card blue"><span class="ico">✓</span><strong>Payment integrity</strong><p>Signatures, webhooks and duplicate events tested.</p></div>
<div class="doc-card purple"><span class="ico">✓</span><strong>Data protection</strong><p>Secrets, uploads, logs and backups reviewed.</p></div>
<div class="doc-card yellow"><span class="ico">✓</span><strong>Operations</strong><p>Errors, monitoring and recovery process verified.</p></div>
</div>

- [ ] No live secrets in tracked files or build artifacts.
- [ ] HTTPS and secure cookie settings verified.
- [ ] Admin and customer authorization tested.
- [ ] Upload validation and storage permissions reviewed.
- [ ] Error responses do not leak secrets or stack traces.
- [ ] Dependency and production configuration review completed.

<div class="doc-callout"><span>⚠️</span><div><b>Certification claims</b><br/>Do not display security certification logos or compliance claims unless UBHAYA has obtained and can substantiate the certification.</div></div>

<div class="doc-footer">UBHAYA FASHIONS · Security handbook · Revisit after authentication, payment, upload or infrastructure changes.</div>
