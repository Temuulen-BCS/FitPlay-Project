# ── Build stage ──────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore (layer-cached)
COPY ["FitPlay.Api/FitPlay.Api.csproj", "FitPlay.Api/"]
COPY ["FitPlay.Domain/FitPlay.Domain.csproj", "FitPlay.Domain/"]
RUN dotnet restore "FitPlay.Api/FitPlay.Api.csproj"

# Copy everything and publish
COPY . .
WORKDIR "/src/FitPlay.Api"
RUN dotnet publish -c Release -o /app/publish --no-restore

# ── Runtime stage ────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Railway injects PORT env var; app reads it in Program.cs
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "FitPlay.Api.dll"]
