FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY FitPlay.Domain/FitPlay.Domain.csproj FitPlay.Domain/
COPY FitPlay.Api/FitPlay.Api.csproj FitPlay.Api/
RUN dotnet restore FitPlay.Api/FitPlay.Api.csproj

# Copy everything and build
COPY FitPlay.Domain/ FitPlay.Domain/
COPY FitPlay.Api/ FitPlay.Api/
RUN dotnet publish FitPlay.Api/FitPlay.Api.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Railway sets PORT env var at runtime; use shell form so it expands
CMD ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} dotnet FitPlay.Api.dll
