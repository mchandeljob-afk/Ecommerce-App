using Microsoft.AspNetCore.Mvc;
using SmartCartApi.DTOs;
using SmartCartApi.Services;

namespace SmartCartApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;

    public CartController(ICartService cartService, IOrderService orderService)
    {
        _cartService = cartService;
        _orderService = orderService;
    }

    /// <summary>
    /// Add or update an item in the cart.
    /// </summary>
    [HttpPost("items")]
    [ProducesResponseType(typeof(AddCartItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    k => k.Key,
                    v => v.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );
            return BadRequest(new ApiErrorResponse("Validation failed.", ValidationErrors: errors));
        }

        var (cart, error) = await _cartService.AddOrUpdateItemAsync(request);
        if (error != null)
            return BadRequest(new ApiErrorResponse(error));

        return Ok(new AddCartItemResponse(cart.Id, "Item added to cart successfully."));
    }

    /// <summary>
    /// Get cart details by cart ID.
    /// </summary>
    [HttpGet("{cartId:guid}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCart(Guid cartId)
    {
        var cart = await _cartService.GetCartAsync(cartId);
        if (cart == null)
            return NotFound(new ApiErrorResponse("Cart not found."));

        return Ok(cart);
    }

    /// <summary>
    /// Apply a coupon code to the cart.
    /// </summary>
    [HttpPost("{cartId:guid}/apply-coupon")]
    [ProducesResponseType(typeof(CouponResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApplyCoupon(Guid cartId, [FromBody] ApplyCouponRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ApiErrorResponse("Coupon code is required."));

        var result = await _cartService.ApplyCouponAsync(cartId, request.CouponCode);
        if (!result.Success)
            return BadRequest(new ApiErrorResponse(result.Message));

        return Ok(result);
    }

    /// <summary>
    /// Checkout the cart and create an order.
    /// </summary>
    [HttpPost("{cartId:guid}/checkout")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Checkout(Guid cartId)
    {
        var (order, error) = await _orderService.CheckoutAsync(cartId);
        if (error != null)
        {
            if (error.Contains("stock", StringComparison.OrdinalIgnoreCase))
                return Conflict(new ApiErrorResponse(error));
            return BadRequest(new ApiErrorResponse(error));
        }

        return Ok(order);
    }
}
