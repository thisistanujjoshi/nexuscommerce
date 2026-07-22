# Multi-stage build for the Orders service.
# Build context is the repository root (see docker-compose / CI).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Services/Orders/Orders.Domain/Orders.Domain.csproj src/Services/Orders/Orders.Domain/
COPY src/Services/Orders/Orders.Application/Orders.Application.csproj src/Services/Orders/Orders.Application/
COPY src/Services/Orders/Orders.Infrastructure/Orders.Infrastructure.csproj src/Services/Orders/Orders.Infrastructure/
COPY src/Services/Orders/Orders.Api/Orders.Api.csproj src/Services/Orders/Orders.Api/
RUN dotnet restore src/Services/Orders/Orders.Api/Orders.Api.csproj

COPY src/Services/Orders/ src/Services/Orders/
RUN dotnet publish src/Services/Orders/Orders.Api/Orders.Api.csproj \
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
ENTRYPOINT ["dotnet", "Orders.Api.dll"]
