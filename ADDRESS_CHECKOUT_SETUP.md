# Ubhaya Fashions – Jaipuri-style checkout address drawer

The checkout now uses the right-side cart drawer for address selection.

## Flow
1. Customer opens the cart drawer.
2. Clicks Checkout.
3. Saved addresses load in the same drawer.
4. Customer can select a saved address.
5. Customer can click **Add new address** without leaving the drawer.
6. New address form supports **Use my current location** and Google address search when `Google:MapsApiKey` is configured.
7. Current-location lookup falls back to server-side OpenStreetMap/Nominatim reverse geocoding when Google Maps is not configured.
8. Address is saved to the customer's account and immediately appears in the drawer.
9. Customer continues to Razorpay payment without navigating to another address page.

## Database
Run from `backend`:

```powershell
dotnet ef database update
```

This applies `AddCustomerAddresses`.

## Google Maps (optional but recommended)
In `frontend/appsettings.json` set:

```json
"Google": {
  "MapsApiKey": "YOUR_GOOGLE_MAPS_BROWSER_KEY"
}
```

Enable Places API / Maps JavaScript API for that key and restrict the key to your website origin in production.

If the key is blank, **Use my current location** still works through the backend reverse-geocoding fallback. The fallback is suitable for development/testing; for production, use a geocoding provider with an appropriate service agreement and rate limits.

## Browser permission
The customer must allow location access when the browser asks. The site cannot silently obtain precise GPS location without browser permission.
