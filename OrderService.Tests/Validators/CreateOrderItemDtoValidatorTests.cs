using FluentValidation.TestHelper;
using Xunit;
using OrderService.Core.Models.DTOs;
using OrderService.Core.Validators;

namespace OrderService.Tests.Validators;

public class CreateOrderItemDtoValidatorTests
{
    private readonly CreateOrderItemDtoValidator _validator;

    public CreateOrderItemDtoValidatorTests()
    {
        _validator = new CreateOrderItemDtoValidator();
    }

    #region ProductId Tests

    [Fact]
    public void Validate_ShouldHaveError_WhenProductIdIsEmpty()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.ProductId = "";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("12345678901234567890123")]
    public void Validate_ShouldHaveError_WhenProductIdHasInvalidLength(string productId)
    {
        // Arrange
        var dto = CreateValidDto();
        dto.ProductId = productId;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [Theory]
    [InlineData("507f1f77bcf86cd799439011")]
    [InlineData("507F1F77BCF86CD799439011")]
    public void Validate_ShouldNotHaveError_WhenProductIdIsValid(string productId)
    {
        // Arrange
        var dto = CreateValidDto();
        dto.ProductId = productId;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProductId);
    }

    #endregion

    #region ProductName Tests

    [Fact]
    public void Validate_ShouldHaveError_WhenProductNameIsEmpty()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.ProductName = "";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProductName);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenProductNameExceedsMaxLength()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.ProductName = new string('A', 201);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProductName);
    }

    [Fact]
    public void Validate_ShouldNotHaveError_WhenProductNameIsValid()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.ProductName = "Valid Product Name";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProductName);
    }

    #endregion

    #region UnitPrice Tests

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_ShouldHaveError_WhenUnitPriceIsZeroOrNegative(decimal price)
    {
        // Arrange
        var dto = CreateValidDto();
        dto.UnitPrice = price;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UnitPrice);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenUnitPriceExceedsMaximum()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.UnitPrice = 1000001m;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UnitPrice);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(10.00)]
    [InlineData(999999.99)]
    public void Validate_ShouldNotHaveError_WhenUnitPriceIsValid(decimal price)
    {
        // Arrange
        var dto = CreateValidDto();
        dto.UnitPrice = price;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.UnitPrice);
    }

    #endregion

    #region Quantity Tests

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ShouldHaveError_WhenQuantityIsZeroOrNegative(int quantity)
    {
        // Arrange
        var dto = CreateValidDto();
        dto.Quantity = quantity;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenQuantityExceedsMaximum()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.Quantity = 10001;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(10000)]
    public void Validate_ShouldNotHaveError_WhenQuantityIsValid(int quantity)
    {
        // Arrange
        var dto = CreateValidDto();
        dto.Quantity = quantity;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Quantity);
    }

    #endregion

    #region Helper Methods

    private CreateOrderItemDto CreateValidDto()
    {
        return new CreateOrderItemDto
        {
            ProductId = "507f1f77bcf86cd799439012",
            ProductName = "Test Product",
            UnitPrice = 10.00m,
            Quantity = 1
        };
    }

    #endregion
}
