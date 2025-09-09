# =============================================================================
# Multi-stage Dockerfile for .NET Order Service
# =============================================================================

# -----------------------------------------------------------------------------
# Base stage - Common setup for all stages
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

# Install system dependencies
RUN apt-get update && apt-get install -y \
    curl \
    postgresql-client \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN groupadd -r orderuser && useradd -r -g orderuser orderuser

# -----------------------------------------------------------------------------
# Build stage - Build the application
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies (better caching)
COPY ["OrderService/OrderService.csproj", "OrderService/"]
RUN dotnet restore "OrderService/OrderService.csproj"

# Copy source code and build
COPY . .
RUN dotnet build "OrderService/OrderService.csproj" -c Release -o /app/build

# -----------------------------------------------------------------------------
# Publish stage - Publish the application
# -----------------------------------------------------------------------------
FROM build AS publish
RUN dotnet publish "OrderService/OrderService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# -----------------------------------------------------------------------------
# Development stage - For local development
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS development
WORKDIR /app

# Install system dependencies
RUN apt-get update && apt-get install -y \
    curl \
    postgresql-client \
    && rm -rf /var/lib/apt/lists/*

# Copy project file and restore dependencies
COPY ["OrderService/OrderService.csproj", "OrderService/"]
RUN dotnet restore "OrderService/OrderService.csproj"

# Copy source code
COPY . .

# Create non-root user
RUN groupadd -r orderuser && useradd -r -g orderuser orderuser
RUN chown -R orderuser:orderuser /app
USER orderuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost/health || exit 1

# Expose port
EXPOSE 80
EXPOSE 443

# Run in development mode with hot reload
ENTRYPOINT ["dotnet", "watch", "run", "--project", "OrderService/OrderService.csproj", "--urls", "http://0.0.0.0:80"]

# -----------------------------------------------------------------------------
# Production stage - Optimized for production deployment
# -----------------------------------------------------------------------------
FROM base AS production

# Copy published app
COPY --from=publish --chown=orderuser:orderuser /app/publish .

# Remove unnecessary files for production
RUN rm -rf /tmp/* /var/tmp/*

# Switch to non-root user
USER orderuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost/health || exit 1

# Expose port
EXPOSE 80
EXPOSE 443

# Configure ASP.NET Core
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

# Entry point
ENTRYPOINT ["dotnet", "OrderService.dll"]

# Labels for better image management
LABEL maintainer="AIOutlet Team"
LABEL service="order-service"
LABEL version="1.0.0"
