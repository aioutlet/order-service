#!/bin/bash

# Order Service - Teardown Script
# This script tears down the order service for development

set -e  # Exit on any error

# Teardown options
REMOVE_VOLUMES=false
REMOVE_IMAGES=false

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVICE_DIR="$(dirname "$SCRIPT_DIR")"
SERVICE_NAME="order-service"

# Function to print colored output
print_status() {
    local color=$1
    local message=$2
    echo -e "${color}${message}${NC}"
}

# Function to show help
show_help() {
    echo -e "${YELLOW}Usage: $0 [OPTIONS]${NC}"
    echo ""
    echo -e "${YELLOW}Options:${NC}"
    echo "  -v, --volumes         Remove volumes (⚠️  DATA LOSS!)"
    echo "  -i, --images          Remove Docker images"
    echo "  -h, --help           Show this help message"
    echo ""
    echo -e "${YELLOW}Examples:${NC}"
    echo "  $0                   # Basic teardown"
    echo "  $0 -v                # Remove volumes too (deletes data)"
    echo "  $0 -v -i             # Remove volumes and images"
    echo ""
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -v|--volumes)
            REMOVE_VOLUMES=true
            shift
            ;;
        -i|--images)
            REMOVE_IMAGES=true
            shift
            ;;
        -h|--help)
            show_help
            exit 0
            ;;
        *)
            echo -e "${RED}❌ Error: Unknown option: $1${NC}"
            show_help
            exit 1
            ;;
    esac
done

print_status $BLUE "🧹 Starting $SERVICE_NAME teardown..."

# Check if docker-compose file exists
COMPOSE_FILE="$SERVICE_DIR/docker-compose.yml"
if [ -f "$COMPOSE_FILE" ]; then
    print_status $BLUE "📦 Found docker-compose.yml, stopping services..."
    
    cd "$SERVICE_DIR"
    
    # Stop and remove containers
    if docker-compose down; then
        print_status $GREEN "✅ Containers stopped and removed"
    else
        print_status $YELLOW "⚠️  Some issues stopping containers (they may not be running)"
    fi
    
    # Remove volumes if requested
    if [ "$REMOVE_VOLUMES" = true ]; then
        print_status $BLUE "🗂️  Removing volumes..."
        if docker-compose down -v; then
            print_status $GREEN "✅ Volumes removed"
        else
            print_status $YELLOW "⚠️  Some issues removing volumes"
        fi
    fi
    
    # Remove images if requested
    if [ "$REMOVE_IMAGES" = true ]; then
        print_status $BLUE "📦 Removing images..."
        if docker-compose down --rmi all; then
            print_status $GREEN "✅ Images removed"
        else
            print_status $YELLOW "⚠️  Some issues removing images"
        fi
    fi
else
    print_status $YELLOW "⚠️  No docker-compose.yml found, attempting manual cleanup..."
    
    # Try to remove containers with service name pattern
    CONTAINERS=$(docker ps -aq --filter "name=${SERVICE_NAME}" 2>/dev/null || true)
    if [ -n "$CONTAINERS" ]; then
        print_status $BLUE "🛑 Stopping containers..."
        docker stop $CONTAINERS >/dev/null 2>&1 || true
        docker rm $CONTAINERS >/dev/null 2>&1 || true
        print_status $GREEN "✅ Manual container cleanup completed"
    else
        print_status $BLUE "ℹ️  No containers found matching ${SERVICE_NAME}"
    fi
fi

print_status $GREEN "🧹 $SERVICE_NAME teardown completed!"
print_status $BLUE "ℹ️  Summary:"
print_status $BLUE "  • Containers: Stopped and removed"
if [ "$REMOVE_VOLUMES" = true ]; then
    print_status $BLUE "  • Volumes: Removed (data deleted)"
else
    print_status $BLUE "  • Volumes: Preserved (use -v to remove)"
fi
if [ "$REMOVE_IMAGES" = true ]; then
    print_status $BLUE "  • Images: Removed"
else
    print_status $BLUE "  • Images: Preserved (use -i to remove)"
fi
print_status $BLUE "  • Network: aioutlet-network (shared, not removed)"
