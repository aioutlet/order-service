#!/bin/bash

# Test Order Creation - End-to-End Flow
# This will test: Order Service -> Message Broker -> Order Processor Service -> Status Updates

echo "=========================================="
echo "Testing Order Creation Flow"
echo "=========================================="
echo ""

# Step 1: Login to get JWT token
echo "Step 1: Logging in to get JWT token..."
echo ""

LOGIN_RESPONSE=$(curl -s -X POST http://localhost:3001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john.customer@example.com",
    "password": "Password123!"
  }')

echo "Login Response: $LOGIN_RESPONSE"
echo ""

# Extract token and userId from response
TOKEN=$(echo $LOGIN_RESPONSE | grep -o '"jwt":"[^"]*"' | cut -d'"' -f4)
USER_ID=$(echo $LOGIN_RESPONSE | grep -o '"_id":"[^"]*"' | head -1 | cut -d'"' -f4)

if [ -z "$TOKEN" ]; then
    echo "❌ Failed to get authentication token!"
    echo "Make sure auth-service is running on port 3001"
    echo "And that john.customer@example.com user exists"
    exit 1
fi

echo "✅ Successfully logged in!"
echo "User ID: $USER_ID"
echo "Token: ${TOKEN:0:20}..."
echo ""

# Step 2: Create order with JWT token
echo "Step 2: Creating order for customer: $USER_ID"
echo ""

ORDER_RESPONSE=$(curl -s -X POST http://localhost:5088/api/orders \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d "{
    \"customerId\": \"$USER_ID\",
    \"items\": [
      {
        \"productId\": \"507f191e810c19729de860ea\",
        \"productName\": \"Wireless Mouse\",
        \"unitPrice\": 29.99,
        \"quantity\": 2
      },
      {
        \"productId\": \"507f191e810c19729de860eb\",
        \"productName\": \"USB-C Cable\",
        \"unitPrice\": 12.99,
        \"quantity\": 3
      }
    ],
    \"shippingAddress\": {
      \"addressLine1\": \"123 Main Street\",
      \"addressLine2\": \"Apt 4B\",
      \"city\": \"San Francisco\",
      \"state\": \"CA\",
      \"zipCode\": \"94102\",
      \"country\": \"US\"
    },
    \"billingAddress\": {
      \"addressLine1\": \"123 Main Street\",
      \"addressLine2\": \"Apt 4B\",
      \"city\": \"San Francisco\",
      \"state\": \"CA\",
      \"zipCode\": \"94102\",
      \"country\": \"US\"
    },
    \"notes\": \"Please deliver during business hours\"
  }")

echo "Order Response:"
echo "$ORDER_RESPONSE" | python3 -m json.tool 2>/dev/null || echo "$ORDER_RESPONSE"
echo ""

# Extract order ID from response
ORDER_ID=$(echo $ORDER_RESPONSE | grep -o '"id":"[^"]*"' | cut -d'"' -f4)

if [ -z "$ORDER_ID" ]; then
    echo "❌ Failed to create order!"
    echo "Check the response above for errors"
    exit 1
fi

echo ""
echo "=========================================="
echo "✅ Order created successfully!"
echo "Order ID: $ORDER_ID"
echo ""
echo "Expected Flow:"
echo "1. ✅ Order Service creates order in SQL Server"
echo "2. 🔄 Order Service publishes OrderCreatedEvent to RabbitMQ"
echo "3. 🔄 Order Processor Service receives event"
echo "4. 🔄 Order Processor Service starts saga (payment, inventory, shipping)"
echo "5. 🔄 Order Processor Service publishes OrderStatusChangedEvent"
echo "6. 🔄 Order Service consumer receives status updates"
echo ""
echo "Check the service logs to see the flow in action!"
echo "=========================================="
