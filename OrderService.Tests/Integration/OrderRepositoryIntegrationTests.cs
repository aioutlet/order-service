using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using OrderService.Core.Data;
using OrderService.Core.Repositories;
using OrderService.Core.Models.Entities;
using OrderService.Core.Models.Enums;
using OrderService.Core.Models.DTOs;

namespace OrderService.Tests.Integration;

public class OrderRepositoryIntegrationTests : IDisposable
{
    private readonly OrderDbContext _context;
    private readonly OrderRepository _repository;

    public OrderRepositoryIntegrationTests()
    {
        // Use in-memory database for integration tests
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new OrderDbContext(options);
        var mockLogger = new Mock<ILogger<OrderRepository>>();
        _repository = new OrderRepository(_context, mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region CreateOrderAsync Tests

    [Fact]
    public async Task CreateOrderAsync_ShouldAddOrderToDatabase()
    {
        // Arrange
        var order = CreateTestOrder();

        // Act
        var result = await _repository.CreateOrderAsync(order);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        
        var savedOrder = await _context.Orders.FindAsync(result.Id);
        savedOrder.Should().NotBeNull();
        savedOrder!.OrderNumber.Should().Be(order.OrderNumber);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldSaveOrderWithItems()
    {
        // Arrange
        var order = CreateTestOrder();

        // Act
        var result = await _repository.CreateOrderAsync(order);

        // Assert
        var savedOrder = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == result.Id);
        
        savedOrder.Should().NotBeNull();
        savedOrder!.Items.Should().HaveCount(1);
        savedOrder.Items.First().ProductName.Should().Be("Test Product");
    }

    #endregion

    #region GetAllOrdersAsync Tests

    [Fact]
    public async Task GetAllOrdersAsync_ShouldReturnAllOrders()
    {
        // Arrange
        await _repository.CreateOrderAsync(CreateTestOrder());
        await _repository.CreateOrderAsync(CreateTestOrder());

        // Act
        var result = await _repository.GetAllOrdersAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllOrdersAsync_ShouldReturnOrdersOrderedByCreatedAtDescending()
    {
        // Arrange
        var order1 = CreateTestOrder();
        order1.CreatedAt = DateTime.UtcNow.AddDays(-2);
        await _repository.CreateOrderAsync(order1);

        var order2 = CreateTestOrder();
        order2.CreatedAt = DateTime.UtcNow.AddDays(-1);
        await _repository.CreateOrderAsync(order2);

        // Act
        var result = (await _repository.GetAllOrdersAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].CreatedAt.Should().BeAfter(result[1].CreatedAt);
    }

    #endregion

    #region GetOrderByIdAsync Tests

    [Fact]
    public async Task GetOrderByIdAsync_ShouldReturnOrderWhenExists()
    {
        // Arrange
        var order = await _repository.CreateOrderAsync(CreateTestOrder());

        // Act
        var result = await _repository.GetOrderByIdAsync(order.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(order.Id);
    }

    [Fact]
    public async Task GetOrderByIdAsync_ShouldReturnNullWhenNotExists()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetOrderByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetOrderByIdAsync_ShouldIncludeOrderItems()
    {
        // Arrange
        var order = await _repository.CreateOrderAsync(CreateTestOrder());

        // Act
        var result = await _repository.GetOrderByIdAsync(order.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
    }

    #endregion

    #region GetOrdersByCustomerIdAsync Tests

    [Fact]
    public async Task GetOrdersByCustomerIdAsync_ShouldReturnOrdersForCustomer()
    {
        // Arrange
        var customerId = "customer-123";
        var order1 = CreateTestOrder();
        order1.CustomerId = customerId;
        await _repository.CreateOrderAsync(order1);

        var order2 = CreateTestOrder();
        order2.CustomerId = "different-customer";
        await _repository.CreateOrderAsync(order2);

        // Act
        var result = await _repository.GetOrdersByCustomerIdAsync(customerId);

        // Assert
        result.Should().HaveCount(1);
        result.First().CustomerId.Should().Be(customerId);
    }

    [Fact]
    public async Task GetOrdersByCustomerIdAsync_ShouldReturnEmptyWhenNoOrdersForCustomer()
    {
        // Arrange
        var customerId = "non-existent-customer";

        // Act
        var result = await _repository.GetOrdersByCustomerIdAsync(customerId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region UpdateOrderAsync Tests

    [Fact]
    public async Task UpdateOrderAsync_ShouldUpdateOrderInDatabase()
    {
        // Arrange
        var order = await _repository.CreateOrderAsync(CreateTestOrder());
        order.Status = OrderStatus.Processing;

        // Act
        var result = await _repository.UpdateOrderAsync(order);

        // Assert
        result.Status.Should().Be(OrderStatus.Processing);
        
        var updatedOrder = await _context.Orders.FindAsync(order.Id);
        updatedOrder!.Status.Should().Be(OrderStatus.Processing);
    }

    [Fact]
    public async Task UpdateOrderAsync_ShouldUpdateUpdatedAtTimestamp()
    {
        // Arrange
        var order = await _repository.CreateOrderAsync(CreateTestOrder());
        var originalUpdatedAt = order.UpdatedAt;
        
        await Task.Delay(100); // Small delay to ensure timestamp difference
        order.Status = OrderStatus.Processing;

        // Act
        var result = await _repository.UpdateOrderAsync(order);

        // Assert
        result.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    #endregion

    #region DeleteOrderAsync Tests

    [Fact]
    public async Task DeleteOrderAsync_ShouldRemoveOrderFromDatabase()
    {
        // Arrange
        var order = await _repository.CreateOrderAsync(CreateTestOrder());

        // Act
        var result = await _repository.DeleteOrderAsync(order.Id);

        // Assert
        result.Should().BeTrue();
        
        var deletedOrder = await _context.Orders.FindAsync(order.Id);
        deletedOrder.Should().BeNull();
    }

    [Fact]
    public async Task DeleteOrderAsync_ShouldReturnFalseWhenOrderNotExists()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.DeleteOrderAsync(nonExistentId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetOrdersByStatusAsync Tests

    [Fact]
    public async Task GetOrdersByStatusAsync_ShouldReturnOrdersWithSpecificStatus()
    {
        // Arrange
        var order1 = CreateTestOrder();
        order1.Status = OrderStatus.Processing;
        await _repository.CreateOrderAsync(order1);

        var order2 = CreateTestOrder();
        order2.Status = OrderStatus.Created;
        await _repository.CreateOrderAsync(order2);

        // Act
        var result = await _repository.GetOrdersByStatusAsync(OrderStatus.Processing);

        // Assert
        result.Should().HaveCount(1);
        result.First().Status.Should().Be(OrderStatus.Processing);
    }

    #endregion

    #region GetOrdersPagedAsync Tests

    [Fact]
    public async Task GetOrdersPagedAsync_ShouldReturnPagedResults()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            await _repository.CreateOrderAsync(CreateTestOrder());
        }

        var query = new OrderQueryDto
        {
            Page = 1,
            PageSize = 2
        };

        // Act
        var (orders, totalCount) = await _repository.GetOrdersPagedAsync(query);

        // Assert
        orders.Should().HaveCount(2);
        totalCount.Should().Be(5);
    }

    [Fact]
    public async Task GetOrdersPagedAsync_ShouldFilterByStatus()
    {
        // Arrange
        var order1 = CreateTestOrder();
        order1.Status = OrderStatus.Processing;
        await _repository.CreateOrderAsync(order1);

        var order2 = CreateTestOrder();
        order2.Status = OrderStatus.Created;
        await _repository.CreateOrderAsync(order2);

        var query = new OrderQueryDto
        {
            Status = OrderStatus.Processing,
            Page = 1,
            PageSize = 10
        };

        // Act
        var (orders, totalCount) = await _repository.GetOrdersPagedAsync(query);

        // Assert
        orders.Should().HaveCount(1);
        totalCount.Should().Be(1);
        orders.First().Status.Should().Be(OrderStatus.Processing);
    }

    [Fact]
    public async Task GetOrdersPagedAsync_ShouldFilterByCustomerId()
    {
        // Arrange
        var customerId = "customer-123";
        var order1 = CreateTestOrder();
        order1.CustomerId = customerId;
        await _repository.CreateOrderAsync(order1);

        var order2 = CreateTestOrder();
        order2.CustomerId = "different-customer";
        await _repository.CreateOrderAsync(order2);

        var query = new OrderQueryDto
        {
            CustomerId = customerId,
            Page = 1,
            PageSize = 10
        };

        // Act
        var (orders, totalCount) = await _repository.GetOrdersPagedAsync(query);

        // Assert
        orders.Should().HaveCount(1);
        totalCount.Should().Be(1);
        orders.First().CustomerId.Should().Be(customerId);
    }

    #endregion

    #region GetOrdersByCustomerIdPagedAsync Tests

    [Fact]
    public async Task GetOrdersByCustomerIdPagedAsync_ShouldReturnPagedResults()
    {
        // Arrange
        var customerId = "customer-123";
        for (int i = 0; i < 5; i++)
        {
            var order = CreateTestOrder();
            order.CustomerId = customerId;
            await _repository.CreateOrderAsync(order);
        }

        var pageRequest = new PagedRequestDto
        {
            Page = 1,
            PageSize = 2
        };

        // Act
        var (orders, totalCount) = await _repository.GetOrdersByCustomerIdPagedAsync(customerId, pageRequest);

        // Assert
        orders.Should().HaveCount(2);
        totalCount.Should().Be(5);
    }

    #endregion

    #region Helper Methods

    private Order CreateTestOrder()
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = "507f1f77bcf86cd799439011",
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            Status = OrderStatus.Created,
            PaymentStatus = PaymentStatus.Pending,
            ShippingStatus = ShippingStatus.NotShipped,
            Currency = "USD",
            Subtotal = 100.00m,
            TaxAmount = 8.00m,
            ShippingCost = 10.00m,
            TotalAmount = 118.00m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "TestUser",
            ShippingAddress = new Address
            {
                AddressLine1 = "123 Test St",
                City = "Test City",
                State = "TS",
                ZipCode = "12345",
                Country = "US"
            },
            BillingAddress = new Address
            {
                AddressLine1 = "123 Test St",
                City = "Test City",
                State = "TS",
                ZipCode = "12345",
                Country = "US"
            },
            Items = new List<OrderItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = "507f1f77bcf86cd799439012",
                    ProductName = "Test Product",
                    UnitPrice = 100.00m,
                    Quantity = 1,
                    TotalPrice = 100.00m,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            }
        };
    }

    #endregion
}
