# Order Service - ASP.NET Core 8 Microservice

A robust, production-ready order management microservice built with ASP.NET Core 8, featuring clean architecture, PostgreSQL database, event-driven messaging, and comprehensive API capabilities.

## 🚀 Features

- **Clean Architecture**: Separation of concerns with distinct layers (Controllers, Services, Repositories, Models)
- **Entity Framework Core**: PostgreSQL integration with Code First migrations
- **JWT Authentication**: Token-based authentication with role-based authorization
- **Event-Driven Architecture**: RabbitMQ and Azure Service Bus integration for order events
- **Input Validation**: FluentValidation for comprehensive request validation
- **Error Handling**: Centralized error handling middleware with standardized responses
- **Pagination**: Built-in pagination support for list endpoints
- **API Documentation**: Swagger/OpenAPI documentation with authentication
- **Cross-Service Integration**: MongoDB ObjectId compatibility for microservice communication
- **Production Ready**: Comprehensive logging, configuration management, and error handling

## 🏗️ Architecture

### Project Structure

```
OrderService/
├── Controllers/         # API controllers
├── Services/           # Business logic layer
│   ├── Messaging/      # Message publisher implementations
│   └── IOrderService   # Service interfaces
├── Repositories/       # Data access layer
├── Models/
│   ├── Entities/       # Domain entities
│   ├── DTOs/          # Data transfer objects
│   ├── Events/        # Event contracts
│   └── Enums/         # Enumeration types
├── Data/              # EF Core context and configurations
├── Configuration/     # Application settings classes
├── Validators/        # FluentValidation validators
├── Middlewares/       # Custom middlewares
└── Utils/            # Utility classes
```

### Technology Stack

- **Framework**: ASP.NET Core 8
- **Database**: PostgreSQL with Entity Framework Core
- **Authentication**: JWT Bearer tokens
- **Validation**: FluentValidation
- **Messaging**: RabbitMQ & Azure Service Bus
- **Documentation**: Swagger/OpenAPI
- **Serialization**: System.Text.Json

## 🔧 Configuration

### Environment Setup

1. Copy `.env.example` to `.env`
2. Update the values in `.env` with your configuration

### Database Connection

```bash
ConnectionStrings__DefaultConnection=Host=localhost;Database=orderservice_dev;Username=username;Password=password
```

### JWT Settings

```bash
Jwt__Key=your-secret-key-min-32-characters
Jwt__Issuer=OrderService
Jwt__Audience=OrderService.Users
Jwt__ExpiryInMinutes=60
```

### Message Broker Configuration

```bash
# RabbitMQ
MessageBroker__Provider=RabbitMQ
MessageBroker__RabbitMQ__ConnectionString=amqp://guest:guest@localhost:5672/

# Azure Service Bus (alternative)
MessageBroker__Provider=AzureServiceBus
MessageBroker__AzureServiceBus__ConnectionString=Endpoint=sb://...
```

## 🚀 Getting Started

### Prerequisites

- .NET 8 SDK
- PostgreSQL 12+
- RabbitMQ (optional, for event publishing)
- Azure Service Bus (optional, alternative to RabbitMQ)

### Installation

1. **Clone the repository**

   ```bash
   git clone [repository-url]
   cd order-service
   ```

2. **Install dependencies**

   ```bash
   dotnet restore
   ```

3. **Configure environment**

   ```bash
   cp .env.example .env
   # Edit .env with your settings
   ```

4. **Setup database**

   ```bash
   # Create database
   createdb orderservice_dev

   # Apply migrations
   dotnet ef database update
   ```

5. **Run the application**

   ```bash
   dotnet run
   ```

6. **Access Swagger UI**
   - Navigate to `https://localhost:5001/swagger`

## 📋 API Endpoints

### Core Endpoints

| Method | Endpoint                            | Description                   | Auth Required |
| ------ | ----------------------------------- | ----------------------------- | ------------- |
| GET    | `/`                                 | Health check and service info | No            |
| GET    | `/api/orders`                       | Get paginated orders          | Yes           |
| POST   | `/api/orders`                       | Create new order              | Yes           |
| GET    | `/api/orders/{id}`                  | Get order by ID               | Yes           |
| PUT    | `/api/orders/{id}/status`           | Update order status           | Yes           |
| GET    | `/api/orders/customer/{customerId}` | Get orders by customer        | Yes           |
| GET    | `/api/orders/search`                | Search orders with filters    | Yes           |

### Authentication

- All endpoints (except health check) require JWT authentication
- Include `Authorization: Bearer {token}` header
- Roles supported: `customer`, `admin`

## 🔄 Event-Driven Architecture

The service publishes events to a configurable message broker when significant order operations occur:

### Events Published

- **OrderCreatedEvent**: Published when a new order is created

### Message Broker Support

- **RabbitMQ**: Topic-based routing with configurable exchanges
- **Azure Service Bus**: Topic/subscription pattern with managed identity support
- **Configurable**: Switch between providers via configuration

## 🧪 Testing

### API Testing

See [API_TESTING.md](API_TESTING.md) for comprehensive API testing examples.

### Sample Test Data

```json
{
  "customerId": "507f1f77bcf86cd799439011",
  "productId": "507f1f77bcf86cd799439012"
}
```

## 🛠️ Development

### Using VS Code Debug

- Use the "Debug Order Service" configuration for standard debugging
- Use "Debug Order Service (Hot Reload)" for development with automatic reloading

### Database Migrations

```bash
# Add migration
dotnet ef migrations add MigrationName

# Apply migration
dotnet ef database update

# Remove last migration
dotnet ef migrations remove
```

## 🔒 Security

- JWT token authentication
- Role-based authorization
- Input validation with FluentValidation
- SQL injection protection via EF Core
- HTTPS enforcement in production
- Structured error responses (no sensitive data exposure)

## 📊 Monitoring & Logging

- Comprehensive logging using `ILogger`
- Structured logging with correlation IDs
- Error tracking and debugging support
- Performance monitoring capabilities
- Health check endpoints

## 🚀 Deployment

### Environment Variables

- `ASPNETCORE_ENVIRONMENT`: Environment name
- `ConnectionStrings__DefaultConnection`: Database connection
- `Jwt__Key`: JWT signing key
- `MessageBroker__Provider`: Message broker provider

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🆘 Support

For questions and support:

- Check the [API Testing Guide](API_TESTING.md)
- Review the Swagger documentation
- Check application logs for debugging
- Create an issue for bug reports or feature requests

---

**Built with ❤️ using ASP.NET Core 8**
