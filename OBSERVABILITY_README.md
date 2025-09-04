# Enhanced Logging and Distributed Tracing - Order Service

This document outlines the implementation of enhanced logging and distributed tracing in the Order Service, following the same pattern used in the User Service.

## Overview

The Order Service now includes:
- **Structured JSON logging** with the same format as User Service
- **Distributed tracing** with OpenTelemetry
- **Correlation ID tracking** across all operations
- **Enhanced middleware** for request/response logging
- **Business and security event logging**

## Implementation Details

### 1. Observability Infrastructure

#### Directory Structure
```
Observability/
├── Logging/
│   ├── LogEntrySchema.cs      # JSON log format definitions
│   └── EnhancedLogger.cs      # Main logger implementation
├── Tracing/
│   ├── TracingHelpers.cs      # Tracing utility functions
│   └── TracingSetup.cs        # OpenTelemetry configuration
└── ObservabilitySetup.cs      # Main setup and configuration
```

### 2. Log Format

The service now produces JSON logs in the same format as the User Service:

```json
{
  "timestamp": "2025-09-04T12:00:00.000Z",
  "level": "INFO",
  "service": "order-service",
  "version": "1.0.0",
  "environment": "Local",
  "correlationId": "abc123-def456",
  "message": "Completed operation: CREATE_ORDER",
  "traceId": "a2d42a0512e9e5a4cea4c4fda66d0ef4",
  "spanId": "c27654e46203f693",
  "operation": "CREATE_ORDER",
  "duration": 125,
  "userId": "user123",
  "businessEvent": "ORDER_CREATED",
  "metadata": {
    "trace_id": "a2d42a0512e9e5a4cea4c4fda66d0ef4",
    "span_id": "c27654e46203f693",
    "trace_flags": "01",
    "orderId": "order-456",
    "totalAmount": 149.99
  }
}
```

### 3. Key Features

#### Enhanced Logger
- **Structured logging** with consistent JSON format
- **Tracing integration** with automatic trace/span ID capture
- **Operation tracking** with start/complete/failed patterns
- **Business event logging** for important domain events
- **Security event logging** for suspicious activities
- **Performance monitoring** with duration tracking

#### Distributed Tracing
- **OpenTelemetry integration** with ASP.NET Core, HTTP, and EF Core instrumentation
- **OTLP export** to observability backends (Jaeger, etc.)
- **Activity-based tracing** following .NET conventions
- **Custom span creation** for business operations

#### Enhanced Middleware
- **Correlation ID management** with header propagation
- **Request/response logging** with tracing context
- **Performance tracking** for all HTTP requests
- **Error handling** with proper trace correlation

### 4. Usage Examples

#### In Controllers
```csharp
public class OrdersController : ControllerBase
{
    private readonly EnhancedLogger _logger;
    
    public async Task<ActionResult> CreateOrder(CreateOrderDto dto)
    {
        var correlationId = GetCorrelationId();
        var stopwatch = _logger.OperationStart("CREATE_ORDER", correlationId);
        
        try
        {
            var order = await _orderService.CreateOrderAsync(dto, correlationId);
            
            _logger.OperationComplete("CREATE_ORDER", stopwatch, correlationId, new {
                orderId = order.Id,
                totalAmount = order.TotalAmount
            });
            
            _logger.Business("ORDER_CREATED", correlationId, new {
                orderId = order.Id,
                customerId = order.CustomerId
            });
            
            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.OperationFailed("CREATE_ORDER", stopwatch, ex, correlationId);
            throw;
        }
    }
}
```

#### In Services
```csharp
public class OrderService : IOrderService
{
    private readonly EnhancedLogger _logger;
    
    public async Task<OrderDto> CreateOrderAsync(CreateOrderDto dto, string correlationId)
    {
        var stopwatch = _logger.OperationStart("CREATE_ORDER", correlationId);
        
        try
        {
            // Business logic here
            var order = await _repository.CreateAsync(entity);
            
            _logger.OperationComplete("CREATE_ORDER", stopwatch, correlationId, new {
                orderId = order.Id
            });
            
            return order;
        }
        catch (Exception ex)
        {
            _logger.OperationFailed("CREATE_ORDER", stopwatch, ex, correlationId);
            throw;
        }
    }
}
```

### 5. Configuration

#### Environment Variables
- `ENABLE_TRACING=true` - Enable/disable tracing
- `LOG_LEVEL=DEBUG` - Set minimum log level
- `LOG_TO_CONSOLE=true` - Enable console logging
- `LOG_TO_FILE=true` - Enable file logging
- `LOG_FORMAT=json` - Set log format (json/console)
- `OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4318` - OpenTelemetry endpoint

#### appsettings.json
```json
{
  "ServiceName": "order-service",
  "ServiceVersion": "1.0.0",
  "OpenTelemetry": {
    "OtlpEndpoint": "http://localhost:4318"
  }
}
```

### 6. Log Files

Logs are written to:
- **Development**: `./logs/order-service-local.log`
- **Production**: `/app/logs/order-service-production.log`
- **Errors**: Separate error log files (e.g., `order-service-local-errors.log`)

### 7. Package Dependencies

Added the following NuGet packages:
```xml
<!-- OpenTelemetry for distributed tracing -->
<PackageReference Include="OpenTelemetry" Version="1.9.0" />
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.9.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.9.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.9.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.EntityFrameworkCore" Version="1.0.0-beta.12" />
<PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.9.0" />
```

### 8. Benefits

- **Consistency**: Same log format across all services
- **Traceability**: Full request tracing with correlation IDs
- **Observability**: Rich structured data for monitoring
- **Debugging**: Easy correlation of related operations
- **Performance**: Built-in operation timing
- **Security**: Automatic security event detection
- **Business Intelligence**: Business event tracking

## Next Steps

1. **Deploy and Test**: Run the service to verify logging output
2. **Monitoring Setup**: Configure log aggregation and alerting
3. **Dashboard Creation**: Build observability dashboards
4. **Other Services**: Apply the same pattern to remaining services
5. **Documentation**: Update operational runbooks with new log format

This implementation ensures the Order Service follows the same observability patterns as the User Service, providing consistent monitoring and debugging capabilities across the distributed system.
