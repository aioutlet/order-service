using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using FluentValidation;
using OrderService.Data;
using OrderService.Configuration;
using OrderService.Repositories;
using OrderService.Services;
using OrderService.Services.Messaging;
using OrderService.Middlewares;
using OrderService.Validators;

var builder = WebApplication.CreateBuilder(args);

// Add configuration settings
builder.Services.Configure<OrderServiceSettings>(
    builder.Configuration.GetSection(OrderServiceSettings.SectionName));
builder.Services.Configure<ApiSettings>(
    builder.Configuration.GetSection(ApiSettings.SectionName));
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));

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
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register repositories and services
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService.Services.OrderService>();

// Configure message broker settings
builder.Services.Configure<MessageBrokerSettings>(
    builder.Configuration.GetSection(MessageBrokerSettings.SectionName));

// Register message broker implementations
builder.Services.AddTransient<RabbitMQPublisher>();
builder.Services.AddTransient<AzureServiceBusPublisher>();
builder.Services.AddSingleton<IMessagePublisherFactory, MessagePublisherFactory>();
builder.Services.AddScoped<IMessagePublisher, MessagePublisherService>();

var app = builder.Build();

// Configure the HTTP request pipeline.

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
