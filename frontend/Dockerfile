FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore LadiesDressStore.Web.csproj
RUN dotnet publish LadiesDressStore.Web.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "LadiesDressStore.Web.dll"]
