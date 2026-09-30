# Customer account features

Apply the customer profile and reset-token schema from the backend folder:

```powershell
dotnet ef database update --configuration Release
```

Email password reset requires SMTP settings in the backend environment. Configure these values without committing credentials:

```text
Smtp__Host
Smtp__Port                 (optional; defaults to 587)
Smtp__Username             (optional for unauthenticated relays)
Smtp__Password
Smtp__From
Smtp__EnableSsl            (optional; defaults to true)
```

The reset endpoint sends a one-hour, single-use link and stores only a SHA-256 hash of the token. It returns a generic response whether or not the email belongs to an account.
