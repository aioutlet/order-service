@echo off
setlocal enabledelayedexpansion

echo ==========================================
echo Testing Order Creation Flow
echo ==========================================
echo.

REM Step 1: Login to get JWT token
echo Step 1: Logging in to get JWT token...
echo.

curl -s -X POST http://localhost:3001/api/auth/login ^
  -H "Content-Type: application/json" ^
  -d "{\"email\":\"john.customer@example.com\",\"password\":\"Password123!\"}" > login_response.json

echo Login response saved to login_response.json
echo.

REM Parse token and userId from response (requires jq or manual extraction)
REM For simplicity, we'll use PowerShell to parse JSON
powershell -Command "$json = Get-Content login_response.json | ConvertFrom-Json; $json.jwt" > token.txt
powershell -Command "$json = Get-Content login_response.json | ConvertFrom-Json; $json.user._id" > userid.txt

set /p TOKEN=<token.txt
set /p USER_ID=<userid.txt

if "%TOKEN%"=="" (
    echo Failed to get authentication token!
    echo Make sure auth-service is running on port 3001
    echo And that john.customer@example.com user exists
    pause
    exit /b 1
)

echo Successfully logged in!
echo User ID: %USER_ID%
echo Token: %TOKEN:~0,20%...
echo.

REM Step 2: Create order with JWT token
echo Step 2: Creating order for customer: %USER_ID%
echo.

curl -X POST http://localhost:5088/api/orders ^
  -H "Content-Type: application/json" ^
  -H "Accept: application/json" ^
  -H "Authorization: Bearer %TOKEN%" ^
  -d "{\"customerId\":\"%USER_ID%\",\"items\":[{\"productId\":\"507f191e810c19729de860ea\",\"productName\":\"Wireless Mouse\",\"unitPrice\":29.99,\"quantity\":2},{\"productId\":\"507f191e810c19729de860eb\",\"productName\":\"USB-C Cable\",\"unitPrice\":12.99,\"quantity\":3}],\"shippingAddress\":{\"addressLine1\":\"123 Main Street\",\"addressLine2\":\"Apt 4B\",\"city\":\"San Francisco\",\"state\":\"CA\",\"zipCode\":\"94102\",\"country\":\"US\"},\"billingAddress\":{\"addressLine1\":\"123 Main Street\",\"addressLine2\":\"Apt 4B\",\"city\":\"San Francisco\",\"state\":\"CA\",\"zipCode\":\"94102\",\"country\":\"US\"},\"notes\":\"Please deliver during business hours\"}"

echo.
echo.
echo ==========================================
echo Order created successfully!
echo.
echo Expected Flow:
echo 1. Order Service creates order in SQL Server
echo 2. Order Service publishes OrderCreatedEvent to RabbitMQ
echo 3. Order Processor Service receives event
echo 4. Order Processor Service starts saga (payment, inventory, shipping)
echo 5. Order Processor Service publishes OrderStatusChangedEvent
echo 6. Order Service consumer receives status updates
echo.
echo Check the service logs to see the flow in action!
echo ==========================================
echo.

REM Cleanup
del login_response.json token.txt userid.txt 2>nul

pause
