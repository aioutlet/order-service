using FluentValidation;
using OrderService.Models.DTOs;
using OrderService.Models.Enums;

namespace OrderService.Validators;

/// <summary>
/// Validator for UpdateOrderStatusDto
/// </summary>
public class UpdateOrderStatusDtoValidator : AbstractValidator<UpdateOrderStatusDto>
{
    public UpdateOrderStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Invalid order status");
    }
}

/// <summary>
/// Validator for OrderQueryDto
/// </summary>
public class OrderQueryDtoValidator : AbstractValidator<OrderQueryDto>
{
    public OrderQueryDtoValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100)
            .WithMessage("Page size cannot exceed 100");

        RuleFor(x => x.Status)
            .IsInEnum()
            .When(x => x.Status.HasValue)
            .WithMessage("Invalid order status");

        RuleFor(x => x.CustomerId)
            .Length(24)
            .WithMessage("Customer ID must be 24 characters (MongoDB ObjectId)")
            .Matches("^[0-9a-fA-F]{24}$")
            .WithMessage("Customer ID must be a valid MongoDB ObjectId")
            .When(x => !string.IsNullOrEmpty(x.CustomerId));

        RuleFor(x => x.OrderDateFrom)
            .LessThanOrEqualTo(x => x.OrderDateTo)
            .When(x => x.OrderDateFrom.HasValue && x.OrderDateTo.HasValue)
            .WithMessage("Order date from must be less than or equal to order date to");

        RuleFor(x => x.SortBy)
            .IsInEnum()
            .WithMessage("Invalid sort option");
    }
}

/// <summary>
/// Validator for PagedRequestDto
/// </summary>
public class PagedRequestDtoValidator : AbstractValidator<PagedRequestDto>
{
    public PagedRequestDtoValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100)
            .WithMessage("Page size cannot exceed 100");
    }
}
