# syntax=docker/dockerfile:1

# ── Стадия сборки: SDK .NET 8 ────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Сначала только solution и csproj-файлы — чтобы слой restore кэшировался,
# пока не меняются зависимости.
COPY InstagramClone.sln ./
COPY Domain/Domain.csproj Domain/
COPY Infrastructure/Infrastructure.csproj Infrastructure/
COPY WebApi/WebApi.csproj WebApi/
RUN dotnet restore WebApi/WebApi.csproj

# Остальной исходник + публикация.
COPY . .
RUN dotnet publish WebApi/WebApi.csproj -c Release -o /app/publish /p:UseAppHost=false

# ── Стадия рантайма: ASP.NET 8 (без SDK, образ меньше) ───────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

# Render передаёт порт через переменную PORT (Program.cs её читает).
# EXPOSE — информационный; Render определяет порт из PORT.
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "WebApi.dll"]
