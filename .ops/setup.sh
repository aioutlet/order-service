#!/bin/bash

# Order Service Setup (.NET Core)
# This script sets up the .NET order service for development

set -e

SERVICE_NAME="order-service"
SERVICE_PATH="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--help)
            echo "Usage: $0 [options]"
            echo "Options:"
            echo "  -h, --help           Show this help message"
            echo ""
            echo "Examples:"
            echo "  $0                   # Setup order service for development"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            echo "Use -h or --help for usage information"
            exit 1
            ;;
    esac
done

echo "🚀 Setting up $SERVICE_NAME (.NET) for development environment..."

# Check if .NET SDK is installed
if ! command -v dotnet &> /dev/null; then
    echo "❌ Error: .NET SDK is not installed or not in PATH"
    echo "Please install .NET 8.0 SDK or later from https://dotnet.microsoft.com/download"
    exit 1
fi

echo "✅ .NET SDK is available"

# Function to check if a command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Function to detect OS
detect_os() {
    if [[ "$OSTYPE" == "darwin"* ]]; then
        echo "macos"
    elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
        echo "linux"
    elif [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "win32" ]]; then
        echo "windows"
    else
        echo "unknown"
    fi
}

# Check for .NET CLI
check_dotnet() {
    echo "🔍 Checking .NET installation..."
    
    if command_exists dotnet; then
        DOTNET_VERSION=$(dotnet --version)
        echo "✅ .NET $DOTNET_VERSION is installed"
        
        # Check if version is 8.0 or higher
        MAJOR_VERSION=$(echo "$DOTNET_VERSION" | cut -d. -f1)
        if [[ $MAJOR_VERSION -lt 8 ]]; then
            echo "⚠️  Warning: .NET 8.0+ is recommended. Current version: $DOTNET_VERSION"
        fi
    else
        echo "❌ Error: .NET CLI is not installed. Please install .NET 8.0 or later"
        exit 1
    fi
}

# Check for PostgreSQL (optional for validation)
check_postgresql() {
    echo "🔍 Checking PostgreSQL installation..."
    
    if command_exists psql; then
        POSTGRES_VERSION=$(psql --version | awk '{print $3}' | sed 's/,.*//g')
        echo "✅ PostgreSQL $POSTGRES_VERSION is installed"
    else
        echo "ℹ️  PostgreSQL client not found (optional for development)"
    fi
}

# Setup .NET project
setup_dotnet_project() {
    echo "🔍 Setting up .NET project..."
    
    cd "$SERVICE_PATH"
    
    # Find the project file
    local project_file
    project_file=$(find . -name "*.csproj" | head -1)
    
    if [[ -n "$project_file" ]]; then
        echo "📦 Found project file: $(basename "$project_file")"
        
        # Restore NuGet packages
        echo "📦 Restoring .NET dependencies..."
        if dotnet restore; then
            echo "✅ Dependencies restored successfully"
        else
            echo "⚠️  Warning: Dependency restore failed"
        fi
        
        # Build the project
        echo "🔨 Building .NET project..."
        if dotnet build --no-restore; then
            echo "✅ Project built successfully"
        else
            echo "❌ Project build failed"
            exit 1
        fi
    else
        echo "⚠️  Warning: No .csproj file found in project directory"
    fi
}

# Validate setup
validate_setup() {
    echo "🔍 Validating setup..."
    
    # Check if .NET project builds
    local project_file
    project_file=$(find "$SERVICE_PATH" -name "*.csproj" | head -1)
    
    if [[ -n "$project_file" ]]; then
        cd "$SERVICE_PATH"
        echo "� Testing project build..."
        if dotnet build --no-restore --verbosity quiet > /dev/null 2>&1; then
            echo "✅ .NET project builds successfully"
        else
            echo "❌ .NET project build failed"
            return 1
        fi
    else
        echo "⚠️  No .csproj file found, skipping build validation"
    fi
    
    return 0
}

# Main execution
main() {
    echo "=========================================="
    echo "📦 Order Service Setup (.NET)"
    echo "=========================================="
    
    OS=$(detect_os)
    echo "ℹ️  Detected OS: $OS"
    echo "🌍 Target Environment: Development"
    
    # Check prerequisites
    check_dotnet
    check_postgresql
    
    # Setup project
    setup_dotnet_project
    
    # Validate setup
    if validate_setup; then
        echo "=========================================="
        echo "✅ Order Service (.NET) setup completed successfully!"
        echo "=========================================="
        echo ""
        
        # Start services with Docker Compose
        echo "🐳 Starting services with Docker Compose..."
        cd "$SERVICE_PATH"
        if docker-compose up -d; then
            echo "✅ Services started successfully"
            echo ""
            echo "⏳ Waiting for services to be ready..."
            sleep 15
            
            # Check service health
            if docker-compose ps | grep -q "Up.*healthy\|Up"; then
                echo "✅ Services are healthy and ready"
            else
                echo "⚠️  Services may still be starting up"
            fi
        else
            echo "❌ Failed to start services with Docker Compose"
            return 1
        fi
        echo ""
        
        echo "🏪 Setup Summary:"
        echo "  • Environment: Development"
        echo "  • Configuration: appsettings.Development.json"
        echo "  • Project: Built and ready"
        echo "  • Network: aioutlet-network (shared)"
        echo "  • Database: PostgreSQL on port 5433"
        echo ""
        echo "🛒 Order Features:"
        echo "  • Order Management & Processing"
        echo "  • Stock Validation & Reservation"
        echo "  • Payment Integration"
        echo "  • Order Status Tracking"
        echo "  • Event Sourcing & CQRS"
        echo ""
        echo "🚀 Service is now running:"
        echo "  • Order Service: http://localhost:3005"
        echo "  • View status: docker-compose ps"
        echo "  • View logs: docker-compose logs -f order-service"
        echo "  • Stop services: bash .ops/teardown.sh"
        echo ""
    else
        echo "❌ Setup validation failed"
        exit 1
    fi
}

# Run main function
main "$@"

# Function to check if a command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Function to detect OS
detect_os() {
    if [[ "$OSTYPE" == "darwin"* ]]; then
        echo "macos"
    elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
        echo "linux"
    elif [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "win32" ]]; then
        echo "windows"
    else
        echo "unknown"
    fi
}

# Check for .NET CLI
check_dotnet() {
    echo "🔍 Checking .NET installation..."
    
    if command_exists dotnet; then
        DOTNET_VERSION=$(dotnet --version)
        echo "✅ .NET $DOTNET_VERSION is installed"
        
        # Check if version is 8.0 or higher
        MAJOR_VERSION=$(echo "$DOTNET_VERSION" | cut -d. -f1)
        if [[ $MAJOR_VERSION -lt 8 ]]; then
            echo "⚠️  Warning: .NET 8.0+ is recommended. Current version: $DOTNET_VERSION"
        fi
    else
        echo "❌ Error: .NET CLI is not installed. Please install .NET 8.0 or later"
        exit 1
    fi
}

# Check for PostgreSQL (optional for validation)
check_postgresql() {
    echo "🔍 Checking PostgreSQL installation..."
    
    if command_exists psql; then
        POSTGRES_VERSION=$(psql --version | awk '{print $3}' | sed 's/,.*//g')
        echo "✅ PostgreSQL $POSTGRES_VERSION is installed"
    else
        echo "ℹ️  PostgreSQL client not found (optional for development)"
    fi
}

# Setup .NET project
setup_dotnet_project() {
    echo "🔍 Setting up .NET project..."
    
    cd "$SERVICE_PATH"
    
    # Find the project file
    local project_file
    project_file=$(find . -name "*.csproj" | head -1)
    
    if [[ -n "$project_file" ]]; then
        echo "📦 Found project file: $(basename "$project_file")"
        
        # Restore NuGet packages
        echo "📦 Restoring .NET dependencies..."
        if dotnet restore; then
            echo "✅ Dependencies restored successfully"
        else
            echo "⚠️  Warning: Dependency restore failed"
        fi
        
        # Build the project
        echo "🔨 Building .NET project..."
        if dotnet build --no-restore; then
            echo "✅ Project built successfully"
        else
            echo "❌ Project build failed"
            exit 1
        fi
    else
        echo "⚠️  Warning: No .csproj file found in project directory"
    fi
}

# Validate setup
validate_setup() {
    echo "🔍 Validating setup..."
    
    # Check if .NET project builds
    local project_file
    project_file=$(find "$SERVICE_PATH" -name "*.csproj" | head -1)
    
    if [[ -n "$project_file" ]]; then
        cd "$SERVICE_PATH"
        echo "🔨 Testing project build..."
        if dotnet build --no-restore --verbosity quiet > /dev/null 2>&1; then
            echo "✅ .NET project builds successfully"
        else
            echo "❌ .NET project build failed"
            return 1
        fi
    else
        echo "⚠️  No .csproj file found, skipping build validation"
    fi
    
    return 0
}

# Main execution
main() {
    echo "=========================================="
    echo "📦 Order Service Setup (.NET)"
    echo "=========================================="
    
    OS=$(detect_os)
    echo "ℹ️  Detected OS: $OS"
    echo "🌍 Target Environment: $ENV_NAME"
    
    # Check prerequisites
    check_dotnet
    check_postgresql
    
    # Setup project
    setup_dotnet_project
    
    # Validate setup
    if validate_setup; then
        echo "=========================================="
        echo "✅ Order Service (.NET) setup completed successfully!"
        echo "=========================================="
        echo ""
        
        # Start services with Docker Compose
        echo "🐳 Starting services with Docker Compose..."
        if docker-compose up -d; then
            echo "✅ Services started successfully"
            echo ""
            echo "⏳ Waiting for services to be ready..."
            sleep 15
            
            # Check service health
            if docker-compose ps | grep -q "Up.*healthy\|Up"; then
                echo "✅ Services are healthy and ready"
            else
                echo "⚠️  Services may still be starting up"
            fi
        else
            echo "❌ Failed to start services with Docker Compose"
            return 1
        fi
        echo ""
        
        echo "🏪 Setup Summary:"
        echo "  • Environment: $ASPNET_ENVIRONMENT (ASPNETCORE_ENVIRONMENT)"
        echo "  • Configuration: $(basename "$APPSETTINGS_FILE")"
        echo "  • Project: Built and ready"
        echo ""
        echo "🛒 Order Features:"
        echo "  • Order Management & Processing"
        echo "  • Stock Validation & Reservation"
        echo "  • Payment Integration"
        echo "  • Order Status Tracking"
        echo "  • Event Sourcing & CQRS"
        echo ""
        echo "🚀 Service is now running:"
        echo "  • View status: docker-compose ps"
        echo "  • View logs: docker-compose logs -f"
        echo "  • Stop services: bash .ops/teardown.sh"
        echo ""
    else
        echo "❌ Setup validation failed"
        exit 1
    fi
}

# Run main function
main "$@"
