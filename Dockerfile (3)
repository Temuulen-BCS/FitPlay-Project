FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["FitPlay.Api/FitPlay.Api.csproj", "FitPlay.Api/"]
COPY ["FitPlay.Domain/FitPlay.Domain.csproj", "FitPlay.Domain/"]
RUN dotnet restore "FitPlay.Api/FitPlay.Api.csproj"

COPY . .
WORKDIR "/src/FitPlay.Api"
RUN dotnet publish "FitPlay.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FitPlay.Api.dll"]