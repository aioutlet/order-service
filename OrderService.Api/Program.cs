using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using FluentValidation;
using FluentValidation.AspNetCore;
using OrderService.Core.Data;
using OrderService.Core.Repositories;
using OrderService.Core.Services;
using OrderService.Core.Extensions;
using OrderService.Core.Validators;
using OrderService.Core.Utils;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddDapr() // Add Dapr integration
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderDtoValidator>();
builder.Services.AddFluentValidationAutoValidation();

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

// Add Entity Framework with SQL Server - Lazy load connection string from Dapr secrets
builder.Services.AddDbContext<OrderDbContext>((serviceProvider, options) =>
{
    var secretService = serviceProvider.GetRequiredService<DaprSecretService>();
    var connectionString = secretService.GetDatabaseConnectionStringAsync().GetAwaiter().GetResult();
    options.UseSqlServer(
        connectionString,
        b => b.MigrationsAssembly("OrderService.Api"));
});

// Register repositories and services
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService.Core.Services.OrderService>();

// Register current user service for JWT authentication
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Register StandardLogger
builder.Services.AddSingleton<StandardLogger>();

// Register Dapr services
builder.Services.AddSingleton<DaprSecretService>();
builder.Services.AddSingleton<DaprEventPublisher>();

Console.WriteLine("Starting Order Service API with Dapr integration...");

var app = builder.Build();

// Note: Database migrations are NOT run at startup to avoid Dapr timing issues.
// The database should be migrated separately using: dotnet ef database update
// The DbContext will connect lazily when first accessed, at which point Dapr will be ready.

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add W3C Trace Context middleware
app.UseTraceContext();

// Add Authentication and Authorization middleware
app.UseOrderServiceAuthentication();

// Enable Dapr CloudEvents for publishing
app.UseCloudEvents();

app.MapControllers();

app.Run();
