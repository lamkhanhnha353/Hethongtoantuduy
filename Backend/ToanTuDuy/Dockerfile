# syntax=docker/dockerfile:1

# ─────────────────────────────────────────────────────────────────────────────
# Build stage: restore + publish ASP.NET Core API
# ─────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy riêng file project để tận dụng cache layer khi restore
COPY ToanTuDuy.csproj ./
RUN dotnet restore ToanTuDuy.csproj

COPY . .
RUN dotnet publish ToanTuDuy.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ─────────────────────────────────────────────────────────────────────────────
# Migrations stage: dùng cho service "migrate" trong docker-compose
# Chạy: dotnet ef database update
# ─────────────────────────────────────────────────────────────────────────────
FROM build AS migrations
RUN dotnet tool install --global dotnet-ef --version 10.0.12
ENV PATH="$PATH:/root/.dotnet/tools"
ENTRYPOINT ["dotnet-ef", "database", "update", \
    "--project", "/src/ToanTuDuy.csproj", \
    "--startup-project", "/src/ToanTuDuy.csproj"]

# ─────────────────────────────────────────────────────────────────────────────
# Runtime stage
# ─────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_NOLOGO=true \
    DOTNET_CLI_TELEMETRY_OPTOUT=true

EXPOSE 8080

COPY --from=build /app/publish ./

# Image aspnet:10.0 có sẵn user "app" (UID 1654), không cần chạy root
USER app

ENTRYPOINT ["dotnet", "ToanTuDuy.dll"]
