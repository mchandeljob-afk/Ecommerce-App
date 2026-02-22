using Microsoft.EntityFrameworkCore;
using SmartCartApi.Data;
using SmartCartApi.DTOs;
using SmartCartApi.Models;
using SmartCartApi.Services;

namespace SmartCartApi.Tests;

public class CouponServiceTests
{
    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public void CalculateDiscount_Flat50_ReturnsCorrectDiscount()
    {
        // Arrange
        var context = CreateContext();
        var service = new CouponService(context);
        var coupon = new Coupon
        {
            Code = "FLAT50",
            Type = CouponType.Flat,
            DiscountValue = 50m,
            MinCartSubtotal = 500m,
            IsActive = true
        };

        // Act
        var discount = service.CalculateDiscount(coupon, 600m);

        // Assert
        Assert.Equal(50m, discount);
    }

    [Fact]
    public void CalculateDiscount_Flat50_SubtotalBelowMinimum_ReturnsZero()
    {
        // Arrange
        var context = CreateContext();
        var service = new CouponService(context);
        var coupon = new Coupon
        {
            Code = "FLAT50",
            Type = CouponType.Flat,
            DiscountValue = 50m,
            MinCartSubtotal = 500m,
            IsActive = true
        };

        // Act
        var discount = service.CalculateDiscount(coupon, 400m);

        // Assert
        Assert.Equal(0m, discount);
    }

    [Fact]
    public void CalculateDiscount_Save10_AppliesPercentageWithMaxCap()
    {
        // Arrange
        var context = CreateContext();
        var service = new CouponService(context);
        var coupon = new Coupon
        {
            Code = "SAVE10",
            Type = CouponType.Percentage,
            DiscountValue = 10m,
            MaxDiscount = 200m,
            MinCartSubtotal = 1000m,
            IsActive = true
        };

        // Act - 10% of 3000 = 300, but max is 200
        var discount = service.CalculateDiscount(coupon, 3000m);

        // Assert
        Assert.Equal(200m, discount);
    }

    [Fact]
    public void CalculateDiscount_Save10_BelowMaxCap()
    {
        // Arrange
        var context = CreateContext();
        var service = new CouponService(context);
        var coupon = new Coupon
        {
            Code = "SAVE10",
            Type = CouponType.Percentage,
            DiscountValue = 10m,
            MaxDiscount = 200m,
            MinCartSubtotal = 1000m,
            IsActive = true
        };

        // Act - 10% of 1500 = 150, below max of 200
        var discount = service.CalculateDiscount(coupon, 1500m);

        // Assert
        Assert.Equal(150m, discount);
    }

    [Fact]
    public void ValidateCoupon_NullCoupon_ReturnsInvalid()
    {
        // Arrange
        var context = CreateContext();
        var service = new CouponService(context);

        // Act
        var (isValid, message) = service.ValidateCoupon(null, 600m);

        // Assert
        Assert.False(isValid);
        Assert.Equal("Invalid coupon code.", message);
    }

    [Fact]
    public void ValidateCoupon_InactiveCoupon_ReturnsInvalid()
    {
        // Arrange
        var context = CreateContext();
        var service = new CouponService(context);
        var coupon = new Coupon { Code = "TEST", IsActive = false, MinCartSubtotal = 0 };

        // Act
        var (isValid, message) = service.ValidateCoupon(coupon, 600m);

        // Assert
        Assert.False(isValid);
        Assert.Contains("no longer active", message);
    }

    [Fact]
    public void ValidateCoupon_SubtotalBelowMinimum_ReturnsInvalid()
    {
        // Arrange
        var context = CreateContext();
        var service = new CouponService(context);
        var coupon = new Coupon { Code = "FLAT50", IsActive = true, MinCartSubtotal = 500m };

        // Act
        var (isValid, message) = service.ValidateCoupon(coupon, 300m);

        // Assert
        Assert.False(isValid);
        Assert.Contains("500", message);
    }
}

public class CartServiceTests
{
    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task AddItem_ExceedsStock_ReturnsError()
    {
        // Arrange
        var context = CreateContext();
        var couponService = new CouponService(context);
        var service = new CartService(context, couponService);

        var request = new AddCartItemRequest
        {
            ProductId = 1,
            Quantity = 999 // exceeds stock of 50
        };

        // Act
        var (cart, error) = await service.AddOrUpdateItemAsync(request);

        // Assert
        Assert.NotNull(error);
        Assert.Contains("exceeds available stock", error);
    }

    [Fact]
    public async Task AddItem_ValidRequest_CreatesCart()
    {
        // Arrange
        var context = CreateContext();
        var couponService = new CouponService(context);
        var service = new CartService(context, couponService);

        var request = new AddCartItemRequest
        {
            ProductId = 1,
            Quantity = 2
        };

        // Act
        var (cart, error) = await service.AddOrUpdateItemAsync(request);

        // Assert
        Assert.Null(error);
        Assert.NotNull(cart);
        Assert.Single(cart.Items);
        Assert.Equal(2, cart.Items.First().Quantity);
    }

    [Fact]
    public async Task AddItem_SameProductTwice_UpdatesQuantity()
    {
        // Arrange
        var context = CreateContext();
        var couponService = new CouponService(context);
        var service = new CartService(context, couponService);

        var request1 = new AddCartItemRequest { ProductId = 1, Quantity = 2 };
        var (cart1, _) = await service.AddOrUpdateItemAsync(request1);

        var request2 = new AddCartItemRequest { CartId = cart1.Id, ProductId = 1, Quantity = 3 };

        // Act
        var (cart2, error) = await service.AddOrUpdateItemAsync(request2);

        // Assert
        Assert.Null(error);
        Assert.Single(cart2.Items);
        Assert.Equal(5, cart2.Items.First().Quantity);
    }
}

public class OrderServiceTests
{
    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task Checkout_EmptyCart_ReturnsError()
    {
        // Arrange
        var context = CreateContext();
        var couponService = new CouponService(context);
        var cartService = new CartService(context, couponService);
        var orderService = new OrderService(context, cartService);

        // Act
        var (order, error) = await orderService.CheckoutAsync(Guid.NewGuid());

        // Assert
        Assert.Null(order);
        Assert.Contains("not found", error);
    }

    [Fact]
    public async Task Checkout_ValidCart_CalculatesCorrectPricing()
    {
        // Arrange
        var context = CreateContext();
        var couponService = new CouponService(context);
        var cartService = new CartService(context, couponService);
        var orderService = new OrderService(context, cartService);

        // Add item: Product 1 (Wireless Headphones) = 299.99, qty 2 = 599.98
        var request = new AddCartItemRequest { ProductId = 1, Quantity = 2 };
        var (cart, _) = await cartService.AddOrUpdateItemAsync(request);

        // Apply FLAT50 coupon (subtotal 599.98 >= 500)
        await cartService.ApplyCouponAsync(cart.Id, "FLAT50");

        // Act
        var (order, error) = await orderService.CheckoutAsync(cart.Id);

        // Assert
        Assert.Null(error);
        Assert.NotNull(order);
        Assert.Equal(599.98m, order.Subtotal);
        Assert.Equal(50m, order.Discount);
        Assert.Equal("FLAT50", order.CouponCode);

        // (599.98 - 50) * 0.18 = 549.98 * 0.18 = 99.00 (rounded)
        var expectedAfterDiscount = 549.98m;
        var expectedTax = Math.Round(expectedAfterDiscount * 0.18m, 2);
        Assert.Equal(expectedTax, order.Tax);
        Assert.Equal(expectedAfterDiscount + expectedTax, order.GrandTotal);
    }

    [Fact]
    public async Task Checkout_ReducesProductStock()
    {
        // Arrange
        var context = CreateContext();
        var couponService = new CouponService(context);
        var cartService = new CartService(context, couponService);
        var orderService = new OrderService(context, cartService);

        var originalStock = (await context.Products.FindAsync(1))!.Stock;

        var request = new AddCartItemRequest { ProductId = 1, Quantity = 3 };
        var (cart, _) = await cartService.AddOrUpdateItemAsync(request);

        // Act
        await orderService.CheckoutAsync(cart.Id);

        // Assert
        var product = await context.Products.FindAsync(1);
        Assert.Equal(originalStock - 3, product!.Stock);
    }
}
