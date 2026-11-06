# Customer email verification

Storefront signup creates the customer account immediately with email verification marked false. The customer can sign in and request verification from **My Profile**.

## Flow

```mermaid
%%{init: {"theme":"base","themeVariables":{"background":"#fbf8fc","primaryColor":"#fff7f5","primaryTextColor":"#302522","primaryBorderColor":"#68142e","lineColor":"#68142e","edgeLabelBackground":"#fbf8fc","actorBkg":"#fff7f5","actorBorder":"#68142e","actorTextColor":"#302522","actorLineColor":"#68142e","signalColor":"#68142e","signalTextColor":"#302522","labelBoxBkgColor":"#fff7f5","labelBoxBorderColor":"#68142e","labelTextColor":"#302522","loopTextColor":"#302522","noteBkgColor":"#fff8e9","noteBorderColor":"#c69a45","noteTextColor":"#302522","activationBkgColor":"#f8e9e9","activationBorderColor":"#68142e","sequenceNumberColor":"#302522"}}}%%
sequenceDiagram
    participant C as Customer
    participant W as Storefront
    participant A as Backend API
    participant S as SMTP
    C->>W: Sign up
    W->>A: POST /api/auth/signup
    A-->>W: Account created; verification optional
    C->>W: Sign in and open profile
    W->>A: Request email verification
    A->>S: Send six-digit code
    A-->>W: Code sent
    C->>W: Enter code
    W->>A: Confirm code
    A-->>W: Email marked verified
```

## Code lifetime and limits

The verification code expires after 10 minutes. Resending is limited to once per minute, and five incorrect attempts are allowed before a new code is required. The backend stores a code hash rather than the plain code.

## SMTP configuration

Configure SMTP on the backend using environment variables or a secret store:

```text
Smtp__Host
Smtp__Port
Smtp__EnableSsl
Smtp__Username
Smtp__Password
Smtp__From
```

For Gmail, use a provider-approved app password where required. Do not put SMTP credentials in frontend settings or commit them to source control.

The customer email verification columns and tables are included in backend EF migrations. The API applies pending migrations at startup; use `dotnet ef database update --project backend/backend.csproj --startup-project backend/backend.csproj` when your deployment manages migrations separately.
