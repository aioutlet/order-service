using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;
using FluentValidation;
using OrderService.Core.Data;
using OrderService.Core.Configuration;
using OrderService.Core.Repositories;
using OrderService.Core.Services;
using OrderService.Core.Services.Messaging;
using OrderService.Core.Services.Messaging.Publishers;
using OrderService.Core.Extensions;
using OrderService.Core.Middlewares;
using OrderService.Core.Validators;
using OrderService.Core.Observability;

var builder = WebApplication.CreateBuilder(args);

// Add observability (logging and tracing) - this should be first
builder.AddObservability();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderDtoValidator>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token in the text input below.\n\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\""
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Add JWT Authentication and Authorization (from OrderService.Core)
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddOrderServiceAuthorization();

// Add Entity Framework with PostgreSQL
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("OrderService.Api")));

// Register repositories and services
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService.Core.Services.OrderService>();

// Register current user service for JWT authentication
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Configure message broker settings
builder.Services.Configure<MessageBrokerSettings>(
    builder.Configuration.GetSection(MessageBrokerSettings.SectionName));

// Register message broker service client for API (publishes via HTTP to message-broker-service)
builder.Services.AddHttpClient<MessageBrokerServiceClient>((serviceProvider, client) =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<MessageBrokerSettings>>().Value;
    
    // Configure HttpClient base address (environment variable overrides config)
    var messageBrokerUrl = Environment.GetEnvironmentVariable("MESSAGE_BROKER_SERVICE_URL") 
        ?? settings.Service.Url;
    client.BaseAddress = new Uri(messageBrokerUrl);
    client.Timeout = TimeSpan.FromSeconds(settings.Service.TimeoutSeconds);
    
    // Add API key if configured (environment variable overrides config)
    var apiKey = Environment.GetEnvironmentVariable("MESSAGE_BROKER_API_KEY") 
        ?? settings.Service.ApiKey;
    if (!string.IsNullOrEmpty(apiKey))
    {
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }
});

// Register message broker adapter factory and adapter for embedded consumer
builder.Services.AddSingleton<MessageBrokerAdapterFactory>();
builder.Services.AddSingleton<IMessageBrokerAdapter>(sp =>
{
    var factory = sp.GetRequiredService<MessageBrokerAdapterFactory>();
    return factory.CreateAdapter();
});

// Register embedded consumer as hosted service (replaces OrderService.Worker)
builder.Services.AddHostedService<OrderService.Api.Consumers.OrderStatusConsumerService>();

Console.WriteLine("Starting Order Service API with embedded consumer (replaces Worker)...");

var app = builder.Build();

// Apply database migrations on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<OrderDbContext>();
        logger.LogInformation("Checking database connection...");
        
        // This will create the database if it doesn't exist and apply all pending migrations
        await context.Database.MigrateAsync();
        
        logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the database");
        throw; // Rethrow to prevent app startup with an invalid database state
    }
}

// Configure the HTTP request pipeline.

// Add correlation ID middleware (before error handling)
app.UseCorrelationId();

// Add global error handling middleware
app.UseErrorHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add Authentication and Authorization middleware
app.UseOrderServiceAuthentication();

app.MapControllers();

app.Run();
