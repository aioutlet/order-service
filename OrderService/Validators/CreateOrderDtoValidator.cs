using FluentValidation;
using OrderService.Models.DTOs;

namespace OrderService.Validators;

/// <summary>
/// Validator for CreateOrderDto
/// </summary>
public class CreateOrderDtoValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderDtoValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Customer ID is required")
            .Length(24)
            .WithMessage("Customer ID must be 24 characters (MongoDB ObjectId)")
            .Matches("^[0-9a-fA-F]{24}$")
            .WithMessage("Customer ID must be a valid MongoDB ObjectId");

        RuleFor(x => x.Items)
            .NotNull()
            .WithMessage("Order items are required")
            .NotEmpty()
            .WithMessage("At least one order item is required");

        RuleForEach(x => x.Items)
            .SetValidator(new CreateOrderItemDtoValidator());

        RuleFor(x => x.ShippingAddress)
            .NotNull()
            .WithMessage("Shipping address is required")
            .SetValidator(new AddressDtoValidator());

        RuleFor(x => x.BillingAddress)
            .NotNull()
            .WithMessage("Billing address is required")
            .SetValidator(new AddressDtoValidator());
    }
}

/// <summary>
/// Validator for CreateOrderItemDto
/// </summary>
public class CreateOrderItemDtoValidator : AbstractValidator<CreateOrderItemDto>
{
    public CreateOrderItemDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required")
            .Length(24)
            .WithMessage("Product ID must be 24 characters (MongoDB ObjectId)")
            .Matches("^[0-9a-fA-F]{24}$")
            .WithMessage("Product ID must be a valid MongoDB ObjectId");

        RuleFor(x => x.ProductName)
            .NotEmpty()
            .WithMessage("Product name is required")
            .MaximumLength(200)
            .WithMessage("Product name cannot exceed 200 characters");

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0)
            .WithMessage("Unit price must be greater than 0")
            .LessThan(1000000)
            .WithMessage("Unit price cannot exceed 1,000,000");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0")
            .LessThanOrEqualTo(10000)
            .WithMessage("Quantity cannot exceed 10,000");
    }
}

/// <summary>
/// Validator for AddressDto
/// </summary>
public class AddressDtoValidator : AbstractValidator<AddressDto>
{
    public AddressDtoValidator()
    {
        RuleFor(x => x.AddressLine1)
            .NotEmpty()
            .WithMessage("Address line 1 is required")
            .MaximumLength(200)
            .WithMessage("Address line 1 cannot exceed 200 characters");

        RuleFor(x => x.AddressLine2)
            .MaximumLength(200)
            .WithMessage("Address line 2 cannot exceed 200 characters");

        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage("City is required")
            .MaximumLength(100)
            .WithMessage("City cannot exceed 100 characters");

        RuleFor(x => x.State)
            .NotEmpty()
            .WithMessage("State is required")
            .MaximumLength(100)
            .WithMessage("State cannot exceed 100 characters");

        RuleFor(x => x.ZipCode)
            .NotEmpty()
            .WithMessage("Zip code is required")
            .MaximumLength(20)
            .WithMessage("Zip code cannot exceed 20 characters");

        RuleFor(x => x.Country)
            .NotEmpty()
            .WithMessage("Country is required")
            .MaximumLength(100)
            .WithMessage("Country cannot exceed 100 characters");
    }
}
