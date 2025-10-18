using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using FluentValidation;
using OrderService.Core.Data;
using OrderService.Core.Configuration;
using OrderService.Core.Repositories;
using OrderService.Core.Services;
using OrderService.Core.Services.Messaging;
using OrderService.Api.Middlewares;
using OrderService.Core.Validators;
using OrderService.Api.Observability;

var builder = WebApplication.CreateBuilder(args);

// Add observability (logging and tracing) - this should be first
builder.AddObservability();

// Add configuration settings
builder.Services.Configure<OrderServiceSettings>(
    builder.Configuration.GetSection(OrderServiceSettings.SectionName));
builder.Services.Configure<ApiSettings>(
    builder.Configuration.GetSection(ApiSettings.SectionName));
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));

// Add services to the container.
builder.Services.AddControllers();

// Add rate limiting
builder.Services.AddRateLimitingServices(builder.Configuration);

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

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured")))
        };
    });

// Add Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CustomerOnly", policy => 
        policy.RequireClaim("roles", "customer"));
    
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireClaim("roles", "admin"));
    
    options.AddPolicy("CustomerOrAdmin", policy => 
        policy.RequireAssertion(context =>
            context.User.HasClaim("roles", "customer") || 
            context.User.HasClaim("roles", "admin")));
});

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

// Add rate limiting (after correlation ID, before authentication)
app.UseOrderServiceRateLimiting(builder.Configuration);

// Add global error handling middleware
app.UseErrorHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add Authentication and Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
