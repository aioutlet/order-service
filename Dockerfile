# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy project file and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy source code and build
COPY . ./
RUN dotnet publish -c Release -o out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install PostgreSQL client (optional, for health checks)
RUN apt-get update && apt-get install -y postgresql-client && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/out .

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

# Entry point
ENTRYPOINT ["dotnet", "OrderService.dll"]
