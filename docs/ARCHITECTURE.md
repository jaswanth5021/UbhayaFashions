# Architecture

## System boundaries

```mermaid
%%{init: {"theme":"base","themeVariables":{"background":"#fbf8fc","primaryColor":"#fff7f5","primaryTextColor":"#302522","primaryBorderColor":"#68142e","lineColor":"#68142e","edgeLabelBackground":"#fbf8fc","actorBkg":"#fff7f5","actorBorder":"#68142e","actorTextColor":"#302522","actorLineColor":"#68142e","signalColor":"#68142e","signalTextColor":"#302522","labelBoxBkgColor":"#fff7f5","labelBoxBorderColor":"#68142e","labelTextColor":"#302522","loopTextColor":"#302522","noteBkgColor":"#fff8e9","noteBorderColor":"#c69a45","noteTextColor":"#302522","activationBkgColor":"#f8e9e9","activationBorderColor":"#68142e","sequenceNumberColor":"#302522"}}}%%
flowchart TD
    C[Browser<br/>customer or admin] --> F[ASP.NET Core MVC storefront<br/>renders pages and calls API services]
    F --> A[ASP.NET Core Web API<br/>authenticates and applies business rules]
    A --> D[(SQL Server<br/>EF Core persistence)]
    A --> R[Razorpay]
    A --> H[Delhivery]
    A --> M[SMTP email provider]
    F --> S[Static assets and shared uploads]
    A --> S
    S --> C
    classDef app fill:#fff7f5,stroke:#68142e,color:#302522,stroke-width:2px
    classDef provider fill:#fff8e9,stroke:#c69a45,color:#302522,stroke-width:2px
    classDef data fill:#eefaf4,stroke:#2f7552,color:#302522,stroke-width:2px
    class C,F,A,S app
    class R,H,M provider
    class D data
```

- **Frontend:** `frontend/LadiesDressStore.Web.csproj`, ASP.NET Core MVC on .NET 8. Razor pages serve the storefront and admin UI. `ApiService` and `AdminApiService` call the backend.
- **Backend:** `backend/backend.csproj`, ASP.NET Core Web API on .NET 8. Controllers validate requests and delegate provider work to services.
- **Persistence:** SQL Server through EF Core in `backend/Data/ApplicationDbContext.cs`; migrations are in `backend/Migrations/`.
- **Providers:** Razorpay, Delhivery, SMTP, and optional social sign-in integrations are called from server-side code.
- **Media:** Both applications expose a shared persistent `UploadsPath` under `/uploads`.

## Local and Compose endpoints

| Service | Local launch profile | Docker Compose |
|---|---|---|
| Storefront | `https://localhost:5001` and `http://localhost:5000` | `http://localhost:5173` |
| API | `https://localhost:56738` and `http://localhost:56739` | `http://localhost:5000` |
| SQL Server | Configured local SQL Server | `localhost:1433` |

In local development, the frontend's `ApiBaseUrl` points to `https://localhost:56738/`. Inside Compose, the frontend uses the service address `http://api:5000/`. The host ports differ by environment.

## Request and authentication path

```mermaid
%%{init: {"theme":"base","themeVariables":{"background":"#fbf8fc","primaryColor":"#fff7f5","primaryTextColor":"#302522","primaryBorderColor":"#68142e","lineColor":"#68142e","edgeLabelBackground":"#fbf8fc","fontSize":"16px"},"flowchart":{"useMaxWidth":false,"nodeSpacing":24,"rankSpacing":28,"padding":8}}}%%
flowchart TD
    B[Browser page request] --> F[MVC storefront and ApiService]
    F --> A[Backend API<br/>auth, validation and rules]
    A --> DB[(SQL Server<br/>EF Core read/write)]
    classDef stage fill:#fff7f5,stroke:#68142e,color:#302522,stroke-width:2px
    classDef data fill:#eefaf4,stroke:#2f7552,color:#302522,stroke-width:2px
    class B,F,A stage
    class DB data
```

The API returns JSON to the MVC service layer, which renders the page. Protected API requests use JWT bearer authentication; the frontend uses distinct customer and admin cookies, and admin API authorization is enforced in the backend.

The API applies EF Core migrations during startup, so its configured database must be available. Production deployments should coordinate this startup behavior with their schema migration and rollback process.

## Static files

The storefront applies response compression to the static asset paths (`/css`, `/js`, `/images`, and `/uploads`). Static files receive cache headers; versioned assets get a long cache lifetime. Personalized HTML is not in the static-asset compression branch.
