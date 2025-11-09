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

// Register StandardLogger
builder.Services.AddSingleton<StandardLogger>();

// Register Dapr services
builder.Services.AddSingleton<DaprSecretService>();
builder.Services.AddSingleton<IEventPublisher, DaprEventPublisher>();

Console.WriteLine("Starting Order Service API with Dapr integration...");

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
        
        // Only apply migrations for relational databases (not in-memory)
        var isInMemory = context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
        if (!isInMemory)
        {
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully");
        }
        else
        {
            // For in-memory databases, just ensure it's created
            await context.Database.EnsureCreatedAsync();
            logger.LogInformation("In-memory database created successfully");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while setting up the database");
        throw; // Rethrow to prevent app startup with an invalid database state
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add Authentication and Authorization middleware
app.UseOrderServiceAuthentication();

// Enable Dapr CloudEvents and subscribe handler
app.UseCloudEvents();
app.MapSubscribeHandler();

app.MapControllers();

app.Run();

// Make the implicit Program class public for integration testing
public partial class Program { }
