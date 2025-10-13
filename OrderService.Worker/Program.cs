using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrderService.Core.Data;
using OrderService.Core.Configuration;
using OrderService.Core.Repositories;
using OrderService.Core.Services;
using OrderService.Core.Services.Messaging;
using OrderService.Worker;
using OrderService.Worker.Handlers;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add configuration settings
builder.Services.Configure<OrderServiceSettings>(
    builder.Configuration.GetSection(OrderServiceSettings.SectionName));
builder.Services.Configure<MessageBrokerSettings>(
    builder.Configuration.GetSection(MessageBrokerSettings.SectionName));

// Add Entity Framework with PostgreSQL
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register repositories and services
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService.Core.Services.OrderService>();

// Register RabbitMQ connection service
builder.Services.AddSingleton<IRabbitMQConnectionService, RabbitMQConnectionService>();

// Register event handlers
builder.Services.AddScoped<IEventHandler<OrderService.Worker.Events.OrderCompletedEvent>, OrderCompletedHandler>();
builder.Services.AddScoped<IEventHandler<OrderService.Worker.Events.OrderFailedEvent>, OrderFailedHandler>();
builder.Services.AddScoped<IEventHandler<OrderService.Worker.Events.PaymentProcessedEvent>, PaymentProcessedHandler>();
builder.Services.AddScoped<IEventHandler<OrderService.Worker.Events.InventoryReservedEvent>, InventoryReservedHandler>();
builder.Services.AddScoped<IEventHandler<OrderService.Worker.Events.ShippingPreparedEvent>, ShippingPreparedHandler>();

// Register the worker service (OrderEventListener as BackgroundService)
builder.Services.AddHostedService<OrderEventListenerWorker>();

var host = builder.Build();

Console.WriteLine("Starting Order Service Worker...");
host.Run();
