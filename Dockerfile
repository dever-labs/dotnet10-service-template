# ── Restore ───────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

COPY global.json nuget.config Directory.Build.props Directory.Packages.props ./
COPY src/Api/ServiceTemplate.Api.csproj                             src/Api/
COPY src/Application/ServiceTemplate.Application.csproj             src/Application/
COPY src/Domain/ServiceTemplate.Domain.csproj                       src/Domain/
COPY src/Infrastructure/ServiceTemplate.Infrastructure.csproj       src/Infrastructure/

RUN dotnet restore src/Api/ServiceTemplate.Api.csproj

# ── Build ──────────────────────────────────────────────────────────────────────
FROM restore AS build
ARG VERSION=0.0.1
COPY src/ src/
RUN dotnet build src/Api/ServiceTemplate.Api.csproj \
    -c Release \
    --no-restore \
    -p:Version=${VERSION} \
    -p:TreatWarningsAsErrors=true

# ── Publish ────────────────────────────────────────────────────────────────────
FROM build AS publish
RUN dotnet publish src/Api/ServiceTemplate.Api.csproj \
    -c Release \
    --no-build \
    -o /app/publish \
    -p:UseAppHost=false

# ── Runtime ────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

# Install wget for health check (curl not available in aspnet base image)
USER root
RUN apt-get update && apt-get install -y --no-install-recommends wget && rm -rf /var/lib/apt/lists/*
USER appuser

COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD wget -qO- http://localhost:8080/health || exit 1

EXPOSE 8080
EXPOSE 8081

ENTRYPOINT ["dotnet", "ServiceTemplate.Api.dll"]
