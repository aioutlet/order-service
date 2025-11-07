@echo off
echo.
echo ============================================
echo Starting order-service with Dapr...
echo ============================================
echo.

REM Kill any existing processes on ports
echo Cleaning up existing processes
for /f "tokens=5" %%a in ('netstat -ano ^| findstr :5001 ^| findstr LISTENING') do (
    echo Killing process on port 5001 PID %%a
    taskkill /PID %%a /F >nul 2>&1
)
for /f "tokens=5" %%a in ('netstat -ano ^| findstr :3504 ^| findstr LISTENING') do (
    echo Killing process on port 3504 PID %%a
    taskkill /PID %%a /F >nul 2>&1
)
for /f "tokens=5" %%a in ('netstat -ano ^| findstr :50004 ^| findstr LISTENING') do (
    echo Killing process on port 50004 PID %%a
    taskkill /PID %%a /F >nul 2>&1
)

timeout /t 2 >nul

echo.
echo Starting with Dapr sidecar...
echo App ID: order-service
echo App Port: 5001
echo Dapr HTTP Port: 3504
echo Dapr gRPC Port: 50004
echo.

dapr run ^
  --app-id order-service ^
  --app-port 5001 ^
  --dapr-http-port 3504 ^
  --dapr-grpc-port 50004 ^
  --log-level error ^
  --resources-path ./.dapr ^
  --config ./.dapr/config.yaml ^
  -- dotnet run --project OrderService.Api/OrderService.Api.csproj --urls "http://localhost:5001"

echo.
echo ============================================
echo Service stopped.
echo ============================================
pause
