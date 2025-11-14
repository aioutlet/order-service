using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Xunit;
using FluentAssertions;
using OrderService.Core.Data;
using OrderService.Core.Models.DTOs;
using OrderService.Core.Models.Enums;
using OrderService.Core.Utils;

namespace OrderService.Tests.E2E;

public class OrdersControllerE2ETests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public OrdersControllerE2ETests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Add test configuration with JWT key
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = JwtTestHelper.GetTestSecretKey()
                });
            });

            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext configuration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<OrderDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<OrderDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDatabase" + Guid.NewGuid());
                });

                // Ensure the database is created
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<OrderDbContext>();
                db.Database.EnsureCreated();
            });
        });

        _client = _factory.CreateClient();
    }

    #region Health Check Tests

    [Fact]
    public async Task HealthCheck_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Authentication Tests

    [Fact]
    public async Task GetOrders_ShouldReturnUnauthorized_WhenNoAuthToken()
    {
        // Act
        var response = await _client.GetAsync("/api/orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateOrder_ShouldReturnUnauthorized_WhenNoAuthToken()
    {
        // Arrange
        var createDto = CreateValidOrderDto();

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Integration Tests with Authentication

    [Fact]
    public async Task CreateOrder_ShouldReturnCreated_WithValidAuthToken()
    {
        // Arrange
        var customerId = "507f1f77bcf86cd799439011";
        var token = JwtTestHelper.GenerateTestToken(customerId, "customer");
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var createDto = CreateValidOrderDto();
        createDto.CustomerId = customerId;

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var createdOrder = await response.Content.ReadFromJsonAsync<OrderResponseDto>();
        createdOrder.Should().NotBeNull();
        createdOrder!.CustomerId.Should().Be(customerId);
        createdOrder.Status.Should().Be(OrderStatus.Created);
    }

    [Fact]
    public async Task GetOrder_ShouldReturnOrder_WhenCustomerAccessesOwnOrder()
    {
        // Arrange
        var customerId = "507f1f77bcf86cd799439012";
        var token = JwtTestHelper.GenerateTestToken(customerId, "customer");
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        // Create an order first
        var createDto = CreateValidOrderDto();
        createDto.CustomerId = customerId;
        var createResponse = await _client.PostAsJsonAsync("/api/orders", createDto);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderResponseDto>();

        // Act
        var response = await _client.GetAsync($"/api/orders/{createdOrder!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var order = await response.Content.ReadFromJsonAsync<OrderResponseDto>();
        order.Should().NotBeNull();
        order!.Id.Should().Be(createdOrder.Id);
    }

    [Fact]
    public async Task GetOrder_ShouldReturnForbidden_WhenCustomerAccessesOtherCustomerOrder()
    {
        // Arrange
        // Create an order for customer 1
        var customer1Id = "507f1f77bcf86cd799439013";
        var token1 = JwtTestHelper.GenerateTestToken(customer1Id, "customer");
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token1}");

        var createDto = CreateValidOrderDto();
        createDto.CustomerId = customer1Id;
        var createResponse = await _client.PostAsJsonAsync("/api/orders", createDto);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderResponseDto>();

        // Clear headers and authenticate as customer 2
        _client.DefaultRequestHeaders.Clear();
        var customer2Id = "507f1f77bcf86cd799439014";
        var token2 = JwtTestHelper.GenerateTestToken(customer2Id, "customer");
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token2}");

        // Act
        var response = await _client.GetAsync($"/api/orders/{createdOrder!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetOrder_ShouldReturnOrder_WhenAdminAccessesAnyOrder()
    {
        // Arrange
        // Create an order for a customer
        var customerId = "507f1f77bcf86cd799439015";
        var customerToken = JwtTestHelper.GenerateTestToken(customerId, "customer");
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {customerToken}");

        var createDto = CreateValidOrderDto();
        createDto.CustomerId = customerId;
        var createResponse = await _client.PostAsJsonAsync("/api/orders", createDto);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderResponseDto>();

        // Clear headers and authenticate as admin
        _client.DefaultRequestHeaders.Clear();
        var adminToken = JwtTestHelper.GenerateTestToken("admin-user", "admin");
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {adminToken}");

        // Act
        var response = await _client.GetAsync($"/api/orders/{createdOrder!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var order = await response.Content.ReadFromJsonAsync<OrderResponseDto>();
        order.Should().NotBeNull();
        order!.Id.Should().Be(createdOrder.Id);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task CreateOrder_ShouldReturnBadRequest_WhenInvalidCustomerId()
    {
        // Arrange
        var customerId = "507f1f77bcf86cd799439016";
        var token = JwtTestHelper.GenerateTestToken(customerId, "customer");
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var createDto = CreateValidOrderDto();
        createDto.CustomerId = "invalid"; // Invalid MongoDB ObjectId

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_ShouldReturnBadRequest_WhenEmptyItems()
    {
        // Arrange
        var customerId = "507f1f77bcf86cd799439017";
        var token = JwtTestHelper.GenerateTestToken(customerId, "customer");
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var createDto = CreateValidOrderDto();
        createDto.CustomerId = customerId;
        createDto.Items = new List<CreateOrderItemDto>(); // Empty items list

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Helper Methods

    private CreateOrderDto CreateValidOrderDto()
    {
        return new CreateOrderDto
        {
            CustomerId = "507f1f77bcf86cd799439011",
            Items = new List<CreateOrderItemDto>
            {
                new()
                {
                    ProductId = "507f1f77bcf86cd799439012",
                    ProductName = "Test Product",
                    UnitPrice = 10.00m,
                    Quantity = 1
                }
            },
            ShippingAddress = new AddressDto
            {
                AddressLine1 = "123 Main St",
                City = "Test City",
                State = "TS",
                ZipCode = "12345",
                Country = "US"
            },
            BillingAddress = new AddressDto
            {
                AddressLine1 = "123 Main St",
                City = "Test City",
                State = "TS",
                ZipCode = "12345",
                Country = "US"
            }
        };
    }

    #endregion
}
