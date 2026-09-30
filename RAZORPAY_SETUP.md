# Razorpay Test Payment Setup

## 1. Add your test keys

Update `backend/appsettings.json`:

```json
"Razorpay": {
  "KeyId": "rzp_test_XXXXXXXXXXXXXXXX",
  "KeySecret": "YOUR_TEST_SECRET"
}
```

Do not commit the secret to Git. For a shared/production environment, use environment variables, .NET User Secrets, or a secret vault.

## 2. Create the EF migration

From the `backend` folder:

```powershell
dotnet ef migrations add AddRazorpayPayments
dotnet ef database update
```

The migration adds:
- `Orders.RazorpayOrderId`
- `Payments` table
- indexes for Razorpay order/payment IDs

## 3. Run the applications

Backend:

```powershell
cd backend
dotnet run
```

Frontend:

```powershell
cd frontend
dotnet run --urls "http://localhost:5001"
```

## 4. Payment flow

Cart -> Delivery address -> Razorpay Checkout -> backend signature verification -> Razorpay payment-status verification -> stock reduction -> order marked Processing.

The Razorpay secret is never sent to the browser.
