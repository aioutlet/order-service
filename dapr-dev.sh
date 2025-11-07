#!/bin/bash

echo ""
echo "============================================"
echo "Starting order-service with Dapr..."
echo "============================================"
echo ""

# Kill any existing processes on ports
echo "Cleaning up existing processes..."

# Kill processes on port 5001 (app port)
lsof -ti:5001 | xargs kill -9 2>/dev/null || true

# Kill processes on port 3504 (Dapr HTTP port)
lsof -ti:3504 | xargs kill -9 2>/dev/null || true

# Kill processes on port 50004 (Dapr gRPC port)
lsof -ti:50004 | xargs kill -9 2>/dev/null || true

sleep 2

echo ""
echo "Starting with Dapr sidecar..."
echo "App ID: order-service"
echo "App Port: 5001"
echo "Dapr HTTP Port: 3504"
echo "Dapr gRPC Port: 50004"
echo ""

dapr run \
  --app-id order-service \
  --app-port 5001 \
  --dapr-http-port 3504 \
  --dapr-grpc-port 50004 \
  --log-level info \
  --components-path ./.dapr/components \
  -- dotnet run --project OrderService.Api/OrderService.Api.csproj --urls "http://localhost:5001"

echo ""
echo "============================================"
echo "Service stopped."
echo "============================================"
