# Multi-stage build for the Catalog service.
# Build context is the repository root (see docker-compose / CI).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, as its own layer, so package restore is cached across code changes.
COPY src/Services/Catalog/Catalog.Domain/Catalog.Domain.csproj src/Services/Catalog/Catalog.Domain/
COPY src/Services/Catalog/Catalog.Application/Catalog.Application.csproj src/Services/Catalog/Catalog.Application/
COPY src/Services/Catalog/Catalog.Infrastructure/Catalog.Infrastructure.csproj src/Services/Catalog/Catalog.Infrastructure/
COPY src/Services/Catalog/Catalog.Api/Catalog.Api.csproj src/Services/Catalog/Catalog.Api/
RUN dotnet restore src/Services/Catalog/Catalog.Api/Catalog.Api.csproj

COPY src/Services/Catalog/ src/Services/Catalog/
RUN dotnet publish src/Services/Catalog/Catalog.Api/Catalog.Api.csproj \
    -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
# curl is used by the container healthcheck (the runtime image ships without it).
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app ./

# Run as the non-root user the aspnet image provides.
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
HEALTHCHECK --interval=15s --timeout=5s --retries=10 \
    CMD curl -fsS http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "Catalog.Api.dll"]
