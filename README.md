# Ladies Dress Store - All .NET

This version uses:
- ASP.NET Core MVC frontend
- ASP.NET Core Web API backend
- SQL Server
- Entity Framework Core
- Email/password signup and login
- Google/Facebook OAuth
- Cart
- Orders
- JWT API authentication

No React, Node.js, npm or Vite is required.

## Run backend

cd backend
dotnet restore
dotnet build
dotnet run

API:
http://localhost:5000/swagger

## Run frontend

Open another terminal:

cd frontend
dotnet restore
dotnet build
dotnet run

Use the URL printed by ASP.NET Core.

## Google OAuth

Set in frontend/appsettings.json:
Google:ClientId
Google:ClientSecret

Callback URL must match the frontend URL:
https://localhost:PORT/signin-google
or
http://localhost:PORT/signin-google

## Facebook OAuth

Set:
Facebook:AppId
Facebook:AppSecret

Callback:
https://localhost:PORT/signin-facebook

Do not commit secrets to Git.
