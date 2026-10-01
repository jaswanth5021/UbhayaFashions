# Signup email verification setup

Signup does not require email verification. Customers can log in immediately, then request a 6-digit code from **My Profile** to verify their email. Codes expire after 10 minutes, allow five attempts, and can be resent once per minute.

## Apply the backend database migration

From the backend directory, run:

```powershell
dotnet ef database update
```

This applies the customer email verification status and code fields. Existing customer accounts start as unverified until their owner completes verification.

## Configure Gmail SMTP on the backend

Set these values in the backend environment or local secret store. Do not commit the SMTP password:

```text
Smtp__Host=smtp.gmail.com
Smtp__Port=587
Smtp__EnableSsl=true
Smtp__Username=<Gmail sender address>
Smtp__Password=<Google App Password>
Smtp__From=<Gmail sender address>
```

Use `ubhayafashions@gmail.com` for `Smtp__Username` and `Smtp__From`. Google requires 2-Step Verification before an App Password can be created.
