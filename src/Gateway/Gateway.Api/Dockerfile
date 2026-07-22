# Multi-stage build for the API gateway.
# Build context is the repository root (see docker-compose / CI).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Gateway/Gateway.Api/Gateway.Api.csproj src/Gateway/Gateway.Api/
RUN dotnet restore src/Gateway/Gateway.Api/Gateway.Api.csproj

COPY src/Gateway/ src/Gateway/
RUN dotnet publish src/Gateway/Gateway.Api/Gateway.Api.csproj \
    -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app ./

USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
HEALTHCHECK --interval=15s --timeout=5s --retries=10 \
    CMD curl -fsS http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "Gateway.Api.dll"]
