using Microsoft.EntityFrameworkCore;
using SmartCartApi.Data;
using SmartCartApi.DTOs;
using SmartCartApi.Models;

namespace SmartCartApi.Services;

public interface IOrderService
{
    Task<(OrderDto? Order, string? Error)> CheckoutAsync(Guid cartId);
    Task<OrderDto?> GetOrderAsync(Guid orderId);
}

public class OrderService : IOrderService
{
    private readonly AppDbContext _context;
    private readonly ICartService _cartService;
    private const decimal TaxRate = 0.18m; // 18% GST

    public OrderService(AppDbContext context, ICartService cartService)
    {
        _context = context;
        _cartService = cartService;
    }

    public async Task<(OrderDto? Order, string? Error)> CheckoutAsync(Guid cartId)
    {
        // Use a transaction-like approach for atomicity
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var cart = await _cartService.GetCartEntityAsync(cartId);
            if (cart == null)
                return (null, "Cart not found.");

            if (!cart.Items.Any())
                return (null, "Cannot checkout an empty cart.");

            // Validate stock availability for all items
            var stockErrors = new List<string>();
            foreach (var item in cart.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null)
                {
                    stockErrors.Add($"Product '{item.Product?.Name ?? item.ProductId.ToString()}' is no longer available.");
                    continue;
                }

                if (product.Stock < item.Quantity)
                {
                    stockErrors.Add($"Insufficient stock for '{product.Name}': requested {item.Quantity}, available {product.Stock}. Please reduce quantity or remove item.");
                }
            }

            if (stockErrors.Any())
                return (null, string.Join(" | ", stockErrors));

            // Calculate pricing
            var subtotal = cart.Items.Sum(i => i.Quantity * i.UnitPrice);
            var discount = cart.DiscountAmount;
            var afterDiscount = Math.Max(0, subtotal - discount);
            var tax = Math.Round(afterDiscount * TaxRate, 2);
            var grandTotal = afterDiscount + tax;

            // Create order
            var order = new Order
            {
                Subtotal = subtotal,
                Discount = discount,
                CouponCode = cart.AppliedCouponCode,
                Tax = tax,
                GrandTotal = grandTotal,
                Status = "Confirmed"
            };

            // Create order items and reduce stock atomically
            foreach (var item in cart.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                product!.Stock -= item.Quantity;

                order.Items.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    ProductName = item.Product?.Name ?? product.Name,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    LineTotal = item.Quantity * item.UnitPrice
                });
            }

            _context.Orders.Add(order);

            // Remove cart after successful order
            _context.CartItems.RemoveRange(cart.Items);
            _context.Carts.Remove(cart);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var orderDto = MapToDto(order);
            return (orderDto, null);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return (null, "An error occurred during checkout. Please try again.");
        }
    }

    public async Task<OrderDto?> GetOrderAsync(Guid orderId)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        return order == null ? null : MapToDto(order);
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto(
            order.Id,
            order.Items.Select(i => new OrderItemDto(
                i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.LineTotal
            )).ToList(),
            order.Subtotal,
            order.Discount,
            order.CouponCode,
            order.Tax,
            order.GrandTotal,
            order.Status,
            order.CreatedAt
        );
    }
}
