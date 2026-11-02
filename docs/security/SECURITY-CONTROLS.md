# Ubhaya Fashions — security controls and assurance register

This document records the seven security requirements and distinguishes implemented controls from operational work that still needs evidence. A code change is not a certification, audit, or penetration test.

## 1. Certifications and compliance

**Status: Not certified / evidence review required.**

Do not display ISO 27001, SOC 2, PCI DSS, GDPR, or other certification badges unless a current certificate or independent attestation explicitly covers the legal entity, systems, and scope in question.

Razorpay-hosted checkout should be used for card/UPI collection. Ubhaya Fashions must not store full card numbers, CVV, or payment authentication secrets. Confirm the payment integration and merchant responsibilities with Razorpay. Have the privacy notice, terms, retention schedule, and applicable Indian privacy/e-commerce obligations reviewed by qualified counsel before publishing compliance claims.

Evidence to retain: current vendor agreements, privacy notice approval, data inventory, retention schedule, access review, and any valid third-party certificates.

## 2. Data encryption

**Status: Transport controls added; database/backup encryption requires infrastructure verification.**

- Storefront and API now redirect HTTP to HTTPS outside Development and enable HSTS.
- Customer/admin authentication cookies use Secure in non-Development environments and remain HttpOnly with SameSite=Lax.
- Passwords are stored using BCrypt hashes in the API; never store plaintext passwords.
- API secrets, JWT signing key, database credentials, Razorpay secret, Delhivery token, and SMTP credentials must be supplied through protected environment variables or a secrets manager, never source control.
- Verify SQL Server TDE support and status before enabling it. Use encrypted, access-controlled backups and test restore procedures.

SQL Server verification queries (run as an authorized database administrator):

    SELECT DB_NAME(database_id) AS database_name, encryption_state,
           encryption_state_desc
    FROM sys.dm_database_encryption_keys;

    SELECT name, is_encrypted
    FROM sys.databases
    WHERE database_id = DB_ID();

Do not run TDE enablement scripts until the SQL Server edition/version, certificate backup plan, recovery process, and performance impact have been reviewed.

## 3. Data residency and processors

**Status: AWS region and transfer map not yet evidenced.**

The production EC2 region must be recorded from the AWS console and confirmed against the SQL Server host, backups, logs, object/file storage, and disaster-recovery copies. Do not assume a region based on the business's location.

Maintain a processor register for AWS, Razorpay, Delhivery, transactional email, and optional Google/Facebook sign-in. For each, record data categories, purpose, hosting/processing locations, retention, subprocessors, contractual terms, and cross-border transfers. Update the privacy notice when providers or regions change.

Required deployment record:
- AWS account/environment owner: deployment owner to complete.
- EC2 region: **TBD — verify in AWS Console**.
- SQL Server region and backup region: **TBD — verify**.
- Uploads/media location and backup location: **TBD — verify**.
- Third-party processor locations and transfer terms: **TBD — verify with providers**.

## 4. Access controls

**Status: Existing role separation retained; production access review required.**

- Keep customer and admin identities separate. A customer token must never authorize admin APIs.
- Keep admin accounts individually assigned; remove shared accounts and disable leavers promptly.
- Use least-privilege SQL Server credentials. The application runtime identity should not be sysadmin or db_owner unless a documented migration process temporarily requires it.
- Restrict RDP and SQL Server inbound access to known administrator IPs/VPN. Public inbound access should be limited to required web ports.
- Review admin membership, AWS IAM roles, EC2 security groups, database logins, and secrets at least quarterly and after staff changes.
- Preserve audit logs for admin sign-in, role changes, sensitive order/customer changes, and security events. Redact credentials, tokens, reset links, and payment secrets from logs.

## 5. Vulnerability disclosure

**Status: Repository disclosure policy added.**

See SECURITY.md. Enable GitHub private vulnerability reporting in repository Settings → Security → Code security and analysis. Provide a monitored private security contact before public launch; do not rely on a public support inbox for sensitive reports.

## 6. Incident history and response

**Status: No verified incident register supplied; historical status is unknown.**

Do not publish “zero incidents” until application, AWS, database, email, and payment-provider records have been reviewed for the intended period.

For each suspected incident:
1. Record discovery time, reporter, affected systems, and evidence.
2. Contain the issue (disable compromised credentials, isolate affected hosts, restrict access) without destroying evidence.
3. Preserve relevant logs and snapshots with access controls.
4. Assess data, customer, payment, and operational impact with AWS and affected processors.
5. Obtain legal/privacy advice and make any required notifications within applicable deadlines.
6. Recover from known-good images/backups, rotate secrets, and validate before restoring traffic.
7. Document root cause, remediation, customer communication, and lessons learned.

Maintain a restricted incident register; never put customer personal data or secrets in this public repository.

## 7. Penetration testing and vulnerability management

**Status: Automated CI checks are being introduced; independent penetration test not yet evidenced.**

- Run build and automated tests on every pull request.
- Enable GitHub CodeQL and dependency review workflows.
- Enable Dependabot alerts and security updates in repository settings.
- Run authenticated and unauthenticated DAST against a staging environment before major releases.
- Arrange an independent web/API penetration test before representing the service as penetration-tested. Retest high/critical findings after remediation.
- Track findings with severity, owner, due date, fix commit, and retest evidence.

Suggested release gates: no unresolved Critical/High findings, successful backup restore test, verified TLS/cookie behavior, and a reviewed admin access list.

## Deployment notes for this repository

The deployment currently uses a single website origin, https://ubhaya1.duckdns.org, with the API reverse-proxied on the same origin. CORS is therefore not needed for normal same-origin browser traffic. If the API is later moved to another origin, add only that exact HTTPS origin to Security:AllowedOrigins or FrontendUrl; never use wildcard origins in production.

The domain migration to ubhayafashions.in is a separate controlled change: provision and validate the certificate, update DNS/IIS bindings, OAuth callback URLs, Razorpay webhook/return URLs, email links, allowed origins, and monitoring before switching traffic.

PaymentTesting:Enabled is false by default in the committed frontend settings. Keep it false in every production environment and do not rely on a committed JSON file as the only production override.
