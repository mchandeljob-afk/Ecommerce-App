using System.ComponentModel.DataAnnotations;

namespace SmartCartApi.DTOs;

// Product DTOs
public record ProductDto(int Id, string Name, decimal Price, int Stock, string Description, string Category);

// Cart DTOs
public class AddCartItemRequest
{
    public Guid? CartId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "ProductId must be a positive integer.")]
    public int ProductId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
    public int Quantity { get; set; }
}

public record CartItemDto(int Id, int ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);

public record CartDto(
    Guid Id,
    List<CartItemDto> Items,
    decimal Subtotal,
    string? AppliedCouponCode,
    decimal DiscountAmount,
    decimal Total
);

// Coupon DTOs
public class ApplyCouponRequest
{
    [Required(ErrorMessage = "Coupon code is required.")]
    public string CouponCode { get; set; } = string.Empty;
}

public record CouponResultDto(bool Success, string Message, decimal DiscountAmount, string CouponCode);

// Order DTOs
public record OrderItemDto(int ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);

public record OrderDto(
    Guid OrderId,
    List<OrderItemDto> Items,
    decimal Subtotal,
    decimal Discount,
    string? CouponCode,
    decimal Tax,
    decimal GrandTotal,
    string Status,
    DateTime CreatedAt
);

// Error DTOs
public record ApiErrorResponse(string Error, string? Detail = null, Dictionary<string, string[]>? ValidationErrors = null);

public record AddCartItemResponse(Guid CartId, string Message);
