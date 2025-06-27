# Order Service API Testing

This document provides examples for testing the Order Service API endpoints.

## Prerequisites

1. The service should be running (default port: 5000 for HTTP, 5001 for HTTPS)
2. You'll need a JWT token for authentication (use the JwtTestHelper or configure proper authentication)
3. Ensure PostgreSQL is running and the database is created
4. Configure message broker (RabbitMQ or Azure Service Bus) if testing event publishing

## Environment Setup

### Database Setup

```bash
# Create database (example for local PostgreSQL)
createdb orderservice_dev

# Apply migrations
dotnet ef database update
```

### Configuration

Update `appsettings.Development.json` with your local settings:

- Database connection string
- JWT settings
- Message broker configuration

## API Testing Examples

### 1. Health Check

```bash
curl -X GET "https://localhost:5001/" \
  -H "accept: application/json"
```

### 2. Get JWT Token (for testing)

```bash
# If using the JwtTestHelper for development
curl -X GET "https://localhost:5001/test/jwt?userId=customer123&role=customer" \
  -H "accept: application/json"
```

### 3. Create Order

```bash
curl -X POST "https://localhost:5001/api/orders" \
  -H "accept: application/json" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "customerId": "507f1f77bcf86cd799439011",
    "items": [
      {
        "productId": "507f1f77bcf86cd799439012",
        "productName": "Test Product",
        "unitPrice": 29.99,
        "quantity": 2
      }
    ],
    "shippingAddress": {
      "addressLine1": "123 Main St",
      "addressLine2": "Apt 4B",
      "city": "Anytown",
      "state": "CA",
      "zipCode": "90210",
      "country": "USA"
    },
    "billingAddress": {
      "addressLine1": "123 Main St",
      "addressLine2": "Apt 4B",
      "city": "Anytown",
      "state": "CA",
      "zipCode": "90210",
      "country": "USA"
    }
  }'
```

### 4. Get Orders (Paginated)

```bash
curl -X GET "https://localhost:5001/api/orders?page=1&pageSize=10" \
  -H "accept: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 5. Get Order by ID

```bash
curl -X GET "https://localhost:5001/api/orders/{order-id}" \
  -H "accept: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 6. Get Orders by Customer

```bash
curl -X GET "https://localhost:5001/api/orders/customer/507f1f77bcf86cd799439011" \
  -H "accept: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 7. Update Order Status

```bash
curl -X PUT "https://localhost:5001/api/orders/{order-id}/status" \
  -H "accept: application/json" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "status": "Processing"
  }'
```

### 8. Search Orders with Filters

```bash
curl -X GET "https://localhost:5001/api/orders/search?status=Created&customerId=507f1f77bcf86cd799439011&page=1&pageSize=10" \
  -H "accept: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## Testing Event Publishing

When you create an order, the service will publish an `OrderCreatedEvent` to the configured message broker:

### RabbitMQ

- Check the RabbitMQ Management UI (typically http://localhost:15672)
- Look for the configured exchange and routing key
- Verify that messages are being published

### Azure Service Bus

- Check the Azure Portal
- Navigate to your Service Bus namespace
- Check the configured topic for published messages

## Sample Test Data

### Valid MongoDB ObjectIds for Testing

- Customer ID: `507f1f77bcf86cd799439011`
- Product ID: `507f1f77bcf86cd799439012`
- Product ID: `507f1f77bcf86cd799439013`

### Order Status Values

- `Created`
- `Processing`
- `Shipped`
- `Delivered`
- `Cancelled`
- `Returned`

## Troubleshooting

### Common Issues

1. **Authentication Errors**: Ensure JWT token is valid and not expired
2. **Database Connection**: Verify PostgreSQL is running and connection string is correct
3. **Validation Errors**: Check request payload matches the expected schema
4. **Message Broker Errors**: Verify RabbitMQ/Azure Service Bus is configured and accessible

### Logs

Check the application logs for detailed error information. The service includes comprehensive logging for debugging.

### Error Responses

The API returns standardized error responses with:

- Problem details format (RFC 7807)
- Detailed validation errors
- Structured error information

Example error response:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Error",
  "status": 400,
  "detail": "One or more validation errors occurred",
  "errors": {
    "CustomerId": ["Customer ID must be 24 characters (MongoDB ObjectId)"]
  },
  "timestamp": "2024-12-27T10:30:00Z"
}
```
