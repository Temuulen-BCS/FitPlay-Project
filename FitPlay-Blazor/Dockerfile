# ── Build stage ──────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore (layer-cached)
COPY ["FitPlay-Blazor/FitPlay-Blazor.csproj", "FitPlay-Blazor/"]
COPY ["FitPlay.Api/FitPlay.Api.csproj", "FitPlay.Api/"]
COPY ["FitPlay.Domain/FitPlay.Domain.csproj", "FitPlay.Domain/"]
RUN dotnet restore "FitPlay-Blazor/FitPlay-Blazor.csproj"

# Copy everything and publish
COPY . .
WORKDIR "/src/FitPlay-Blazor"
RUN dotnet publish -c Release -o /app/publish --no-restore

# ── Runtime stage ────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Railway injects PORT; ASP.NET listens on 8080 by default
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
EXPOSE 8080

ENTRYPOINT ["dotnet", "FitPlay-Blazor.dll"]
