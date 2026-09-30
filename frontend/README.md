# .NET Frontend

This replaces the React/Vite frontend.

Requirements:
- .NET 8 SDK
- Existing ASP.NET Core API running on http://localhost:56738

Run:

dotnet restore
dotnet run

Open the URL shown by ASP.NET Core.

Configuration:
- ApiBaseUrl = http://localhost:56738/
- Google credentials under Google:ClientId / Google:ClientSecret
- Facebook credentials under Facebook:AppId / Facebook:AppSecret

Google callback:
http://localhost:<frontend-port>/signin-google

Facebook callback:
http://localhost:<frontend-port>/signin-facebook

For production, use HTTPS and keep OAuth secrets in environment variables or a secret store.
