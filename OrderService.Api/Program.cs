using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using FluentValidation;
using OrderService.Core.Data;
using OrderService.Core.Configuration;
using OrderService.Core.Repositories;
using OrderService.Core.Services;
using OrderService.Core.Services.Messaging;
using OrderService.Core.Extensions;
using OrderService.Core.Middlewares;
using OrderService.Core.Validators;
using OrderService.Core.Observability;

var builder = WebApplication.CreateBuilder(args);

// Add observability (logging and tracing) - this should be first
builder.AddObservability();

// Add services to the container.
builder.Services.AddControllers();

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

// Register HTTP message publisher for API (publishes via HTTP to message-broker-service)
builder.Services.AddHttpClient<IMessagePublisher, HttpMessagePublisher>();

Console.WriteLine("Starting Order Service in API mode...");

var app = builder.Build();

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
