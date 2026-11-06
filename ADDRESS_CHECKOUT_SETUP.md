# Checkout address flow

Checkout lets a signed-in customer choose or manage delivery addresses in the cart drawer.

## Customer flow

```mermaid
%%{init: {"theme":"base","themeVariables":{"background":"#fbf8fc","primaryColor":"#fff7f5","primaryTextColor":"#302522","primaryBorderColor":"#68142e","lineColor":"#68142e","edgeLabelBackground":"#fbf8fc","actorBkg":"#fff7f5","actorBorder":"#68142e","actorTextColor":"#302522","actorLineColor":"#68142e","signalColor":"#68142e","signalTextColor":"#302522","labelBoxBkgColor":"#fff7f5","labelBoxBorderColor":"#68142e","labelTextColor":"#302522","loopTextColor":"#302522","noteBkgColor":"#fff8e9","noteBorderColor":"#c69a45","noteTextColor":"#302522","activationBkgColor":"#f8e9e9","activationBorderColor":"#68142e","sequenceNumberColor":"#302522"}}}%%
flowchart TD
    A[Open cart drawer] --> B[Choose Checkout]
    B --> C[Load saved addresses]
    C --> D{Choose saved address?}
    D -->|Yes| G[Select address]
    D -->|No| E[Add address]
    E --> F[Optional address search or location lookup]
    F --> H[Validate and save address to account]
    H --> G
    G --> I[Continue to payment]
```

Address records are scoped to the signed-in customer. The first saved address becomes the default unless another address is already present; customers can change the default.

## Address lookup

Google Places/address autocomplete is optional and uses the frontend setting `Google:MapsApiKey`. Restrict any browser key to the production site origin and the APIs enabled for the feature.

The current-location action requires the customer's explicit browser permission. When Google Maps is not configured, the application uses a backend OpenStreetMap/Nominatim reverse-geocoding fallback. Treat returned address data as a suggestion; the customer should review and correct it before saving.

## Database

The customer-address schema is included in the backend EF Core migrations. The API applies pending migrations at startup when the database is reachable. To update it manually from the repository root:

```powershell
dotnet ef database update --project backend/backend.csproj --startup-project backend/backend.csproj
```

Relevant API routes are under `/api/addresses` and require the customer's authentication token.
