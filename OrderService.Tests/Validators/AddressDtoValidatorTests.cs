using FluentValidation.TestHelper;
using Xunit;
using OrderService.Core.Models.DTOs;
using OrderService.Core.Validators;

namespace OrderService.Tests.Validators;

public class AddressDtoValidatorTests
{
    private readonly AddressDtoValidator _validator;

    public AddressDtoValidatorTests()
    {
        _validator = new AddressDtoValidator();
    }

    #region AddressLine1 Tests

    [Fact]
    public void Validate_ShouldHaveError_WhenAddressLine1IsEmpty()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.AddressLine1 = "";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AddressLine1);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenAddressLine1ExceedsMaxLength()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.AddressLine1 = new string('A', 201);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AddressLine1);
    }

    [Fact]
    public void Validate_ShouldNotHaveError_WhenAddressLine1IsValid()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.AddressLine1 = "123 Main Street";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.AddressLine1);
    }

    #endregion

    #region AddressLine2 Tests

    [Fact]
    public void Validate_ShouldNotHaveError_WhenAddressLine2IsNull()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.AddressLine2 = null!;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.AddressLine2);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenAddressLine2ExceedsMaxLength()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.AddressLine2 = new string('A', 201);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AddressLine2);
    }

    #endregion

    #region City Tests

    [Fact]
    public void Validate_ShouldHaveError_WhenCityIsEmpty()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.City = "";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.City);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenCityExceedsMaxLength()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.City = new string('A', 101);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.City);
    }

    [Fact]
    public void Validate_ShouldNotHaveError_WhenCityIsValid()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.City = "New York";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.City);
    }

    #endregion

    #region State Tests

    [Fact]
    public void Validate_ShouldHaveError_WhenStateIsEmpty()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.State = "";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.State);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenStateExceedsMaxLength()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.State = new string('A', 101);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.State);
    }

    [Fact]
    public void Validate_ShouldNotHaveError_WhenStateIsValid()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.State = "NY";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.State);
    }

    #endregion

    #region ZipCode Tests

    [Fact]
    public void Validate_ShouldHaveError_WhenZipCodeIsEmpty()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.ZipCode = "";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ZipCode);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenZipCodeExceedsMaxLength()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.ZipCode = new string('1', 21);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ZipCode);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("12345-6789")]
    [InlineData("SW1A 1AA")]
    public void Validate_ShouldNotHaveError_WhenZipCodeIsValid(string zipCode)
    {
        // Arrange
        var dto = CreateValidDto();
        dto.ZipCode = zipCode;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ZipCode);
    }

    #endregion

    #region Country Tests

    [Fact]
    public void Validate_ShouldHaveError_WhenCountryIsEmpty()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.Country = "";

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Country);
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenCountryExceedsMaxLength()
    {
        // Arrange
        var dto = CreateValidDto();
        dto.Country = new string('A', 101);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Country);
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USA")]
    [InlineData("United States")]
    public void Validate_ShouldNotHaveError_WhenCountryIsValid(string country)
    {
        // Arrange
        var dto = CreateValidDto();
        dto.Country = country;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Country);
    }

    #endregion

    #region Helper Methods

    private AddressDto CreateValidDto()
    {
        return new AddressDto
        {
            AddressLine1 = "123 Main St",
            AddressLine2 = "Apt 4B",
            City = "Test City",
            State = "TS",
            ZipCode = "12345",
            Country = "US"
        };
    }

    #endregion
}
