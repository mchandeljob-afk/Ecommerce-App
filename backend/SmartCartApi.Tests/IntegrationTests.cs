using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SmartCartApi.DTOs;

namespace SmartCartApi.Tests;

public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task FullCheckoutFlow_WorksEndToEnd()
    {
        // Step 1: Get products
        var productsResponse = await _client.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.OK, productsResponse.StatusCode);
        var products = await productsResponse.Content.ReadFromJsonAsync<List<ProductDto>>();
        Assert.NotNull(products);
        Assert.True(products.Count > 0);

        // Step 2: Add item to cart
        var addRequest = new AddCartItemRequest { ProductId = 1, Quantity = 2 };
        var addResponse = await _client.PostAsJsonAsync("/api/cart/items", addRequest);
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
        var addResult = await addResponse.Content.ReadFromJsonAsync<AddCartItemResponse>();
        Assert.NotNull(addResult);
        var cartId = addResult.CartId;

        // Step 3: Get cart
        var cartResponse = await _client.GetAsync($"/api/cart/{cartId}");
        Assert.Equal(HttpStatusCode.OK, cartResponse.StatusCode);
        var cart = await cartResponse.Content.ReadFromJsonAsync<CartDto>();
        Assert.NotNull(cart);
        Assert.Single(cart.Items);

        // Step 4: Apply coupon
        var couponRequest = new ApplyCouponRequest { CouponCode = "FLAT50" };
        var couponResponse = await _client.PostAsJsonAsync($"/api/cart/{cartId}/apply-coupon", couponRequest);
        Assert.Equal(HttpStatusCode.OK, couponResponse.StatusCode);

        // Step 5: Checkout
        var checkoutResponse = await _client.PostAsync($"/api/cart/{cartId}/checkout", null);
        Assert.Equal(HttpStatusCode.OK, checkoutResponse.StatusCode);
        var order = await checkoutResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(order);
        Assert.Equal("Confirmed", order.Status);
        Assert.True(order.GrandTotal > 0);

        // Step 6: Get order
        var orderResponse = await _client.GetAsync($"/api/orders/{order.OrderId}");
        Assert.Equal(HttpStatusCode.OK, orderResponse.StatusCode);
        var fetchedOrder = await orderResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(fetchedOrder);
        Assert.Equal(order.OrderId, fetchedOrder.OrderId);
    }

    [Fact]
    public async Task AddItem_InvalidQuantity_ReturnsBadRequest()
    {
        var addRequest = new { ProductId = 1, Quantity = 0 };
        var response = await _client.PostAsJsonAsync("/api/cart/items", addRequest);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ApplyCoupon_InvalidCode_ReturnsBadRequest()
    {
        // First create a cart
        var addRequest = new AddCartItemRequest { ProductId = 1, Quantity = 1 };
        var addResponse = await _client.PostAsJsonAsync("/api/cart/items", addRequest);
        var addResult = await addResponse.Content.ReadFromJsonAsync<AddCartItemResponse>();

        var couponRequest = new ApplyCouponRequest { CouponCode = "INVALID" };
        var response = await _client.PostAsJsonAsync($"/api/cart/{addResult!.CartId}/apply-coupon", couponRequest);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
