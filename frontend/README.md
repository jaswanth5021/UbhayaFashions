# Storefront application

The storefront is an ASP.NET Core MVC application targeting .NET 8. It renders Razor pages and calls the backend API through `ApiService` and `AdminApiService`.

## Run locally

Run both commands from the repository root. Start the API first:

```powershell
dotnet run --project backend/backend.csproj --launch-profile backend
```

Then start the storefront in a second terminal from the repository root:

```powershell
dotnet run --project frontend/LadiesDressStore.Web.csproj --launch-profile LadiesDressStore.Web
```

The development profiles expose the storefront at `https://localhost:5001` and the API at `https://localhost:56738`. The checked-in `ApiBaseUrl` is `https://localhost:56738/`. If you change the API address, update `ApiBaseUrl` in the frontend's local configuration.

## Important settings

Use local secrets/environment variables for real values. Common settings include:

- `ApiBaseUrl`: backend API origin, ending in a slash
- `UploadsPath`: shared persistent media directory served at `/uploads`
- `Google:MapsApiKey`: optional browser key for address autocomplete
- `Google:ClientId` and `Facebook:AppId`: optional external login configuration
- `PublicSiteUrl`: public canonical origin used by generated SEO XML

The frontend has separate customer and admin cookies. Keep API authentication and authorization decisions on the backend.

## Static assets

CSS, JavaScript, images, and uploaded media are served as static assets. Static asset responses use compression and cache headers in the web pipeline. Personalized HTML is intentionally kept out of the static-asset compression branch.
