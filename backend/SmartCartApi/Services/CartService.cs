using Microsoft.EntityFrameworkCore;
using SmartCartApi.Data;
using SmartCartApi.DTOs;
using SmartCartApi.Models;

namespace SmartCartApi.Services;

public interface ICartService
{
    Task<(Cart Cart, string? Error)> AddOrUpdateItemAsync(AddCartItemRequest request);
    Task<CartDto?> GetCartAsync(Guid cartId);
    Task<Cart?> GetCartEntityAsync(Guid cartId);
    Task<CouponResultDto> ApplyCouponAsync(Guid cartId, string couponCode);
    Task RemoveCartAsync(Guid cartId);
}

public class CartService : ICartService
{
    private readonly AppDbContext _context;
    private readonly ICouponService _couponService;

    public CartService(AppDbContext context, ICouponService couponService)
    {
        _context = context;
        _couponService = couponService;
    }

    public async Task<(Cart Cart, string? Error)> AddOrUpdateItemAsync(AddCartItemRequest request)
    {
        var product = await _context.Products.FindAsync(request.ProductId);
        if (product == null)
            return (null!, $"Product with ID {request.ProductId} not found.");

        if (request.Quantity > product.Stock)
            return (null!, $"Requested quantity ({request.Quantity}) exceeds available stock ({product.Stock}) for '{product.Name}'.");

        Cart cart;
        if (request.CartId.HasValue && request.CartId.Value != Guid.Empty)
        {
            cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.Id == request.CartId.Value)
                ?? new Cart();

            if (cart.Id != request.CartId.Value)
            {
                cart = new Cart();
                _context.Carts.Add(cart);
            }
        }
        else
        {
            cart = new Cart();
            _context.Carts.Add(cart);
        }

        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        if (existingItem != null)
        {
            var newQty = existingItem.Quantity + request.Quantity;
            if (newQty > product.Stock)
                return (null!, $"Total quantity ({newQty}) exceeds available stock ({product.Stock}) for '{product.Name}'.");

            existingItem.Quantity = newQty;
            existingItem.UnitPrice = product.Price;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                ProductId = product.Id,
                Product = product,
                Quantity = request.Quantity,
                UnitPrice = product.Price,
                CartId = cart.Id
            });
        }

        // Recalculate discount if coupon is applied
        if (!string.IsNullOrEmpty(cart.AppliedCouponCode))
        {
            var coupon = await _couponService.GetCouponByCodeAsync(cart.AppliedCouponCode);
            var subtotal = cart.Items.Sum(i => i.Quantity * i.UnitPrice);
            if (coupon != null)
            {
                cart.DiscountAmount = _couponService.CalculateDiscount(coupon, subtotal);
            }
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return (cart, null);
    }

    public async Task<CartDto?> GetCartAsync(Guid cartId)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.Id == cartId);

        if (cart == null) return null;

        var items = cart.Items.Select(i => new CartItemDto(
            i.Id, i.ProductId, i.Product.Name, i.Quantity, i.UnitPrice, i.Quantity * i.UnitPrice
        )).ToList();

        var subtotal = items.Sum(i => i.LineTotal);
        var total = Math.Max(0, subtotal - cart.DiscountAmount);

        return new CartDto(cart.Id, items, subtotal, cart.AppliedCouponCode, cart.DiscountAmount, total);
    }

    public async Task<Cart?> GetCartEntityAsync(Guid cartId)
    {
        return await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.Id == cartId);
    }

    public async Task<CouponResultDto> ApplyCouponAsync(Guid cartId, string couponCode)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == cartId);

        if (cart == null)
            return new CouponResultDto(false, "Cart not found.", 0, couponCode);

        if (!cart.Items.Any())
            return new CouponResultDto(false, "Cannot apply coupon to an empty cart.", 0, couponCode);

        var subtotal = cart.Items.Sum(i => i.Quantity * i.UnitPrice);
        var coupon = await _couponService.GetCouponByCodeAsync(couponCode);

        var (isValid, message) = _couponService.ValidateCoupon(coupon, subtotal);
        if (!isValid)
            return new CouponResultDto(false, message, 0, couponCode);

        var discount = _couponService.CalculateDiscount(coupon!, subtotal);
        cart.AppliedCouponCode = coupon!.Code;
        cart.DiscountAmount = discount;
        cart.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new CouponResultDto(true, message, discount, coupon.Code);
    }

    public async Task RemoveCartAsync(Guid cartId)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == cartId);

        if (cart != null)
        {
            _context.CartItems.RemoveRange(cart.Items);
            _context.Carts.Remove(cart);
            await _context.SaveChangesAsync();
        }
    }
}
