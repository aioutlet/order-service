using FluentValidation.TestHelper;
using Xunit;
using OrderService.Core.Models.DTOs;
using OrderService.Core.Validators;

namespace OrderService.Tests.Validators;

public class CreateOrderDtoValidatorTests
{
    private readonly CreateOrderDtoValidator _validator;

    public CreateOrderDtoValidatorTests()
    {
        _validator = new CreateOrderDtoValidator();
    }

    #region CustomerId Tests

    [Fact]
    public void Validate_ShouldHaveError_WhenCustomerIdIsEmpty()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.CustomerId = "";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomerId);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("12345678901234567890123")]
    [InlineData("1234567890123456789012345")]
    public void Validate_ShouldHaveError_WhenCustomerIdHasInvalidLength(string customerId)
    {
        // Arrange
        var dto = CreateValidDto();
        dto.CustomerId = customerId;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomerId);
    }

    [Theory]
    [InlineData("ZZZZZZZZZZZZZZZZZZZZZZZZ")]
    [InlineData("507f1f77bcf86cd79943901G")]
    [InlineData("507f-1f77-bcf86cd-799439")]
    public void Validate_ShouldHaveError_WhenCustomerIdIsNotValidObjectId(string customerId)
    {
        // Arrange
        var dto = CreateValidDto();
        dto.CustomerId = customerId;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomerId);
    }

    [Theory]
    [InlineData("507f1f77bcf86cd799439011")]
    [InlineData("507F1F77BCF86CD799439011")]
    [InlineData("aabbccddeeff001122334455")]
    public void Validate_ShouldNotHaveError_WhenCustomerIdIsValid(string customerId)
    {
        // Arrange
        var dto = CreateValidDto();
        dto.CustomerId = customerId;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CustomerId);
    }

    #endregion

    #region Items Tests

    [Fact]
    public void Validate_ShouldHaveError_WhenItemsIsNull()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.Items = null!;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenItemsIsEmpty()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.Items = new List<CreateOrderItemDto>();

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void Validate_ShouldNotHaveError_WhenItemsAreValid()
    {
        // Arrange
        var dto = CreateValidDto();

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Items);
    }

    #endregion

    #region Address Tests

    [Fact]
    public void Validate_ShouldHaveError_WhenShippingAddressIsNull()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.ShippingAddress = null!;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ShippingAddress);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenBillingAddressIsNull()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.BillingAddress = null!;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BillingAddress);
    }

    #endregion

    #region Helper Methods

    private CreateOrderDto CreateValidDto()
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
