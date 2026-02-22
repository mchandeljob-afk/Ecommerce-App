using Microsoft.EntityFrameworkCore;
using SmartCartApi.Data;
using SmartCartApi.DTOs;
using SmartCartApi.Models;

namespace SmartCartApi.Services;

public interface IProductService
{
    Task<List<ProductDto>> GetAllProductsAsync();
    Task<Product?> GetProductByIdAsync(int productId);
}

public class ProductService : IProductService
{
    private readonly AppDbContext _context;

    public ProductService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductDto>> GetAllProductsAsync()
    {
        return await _context.Products
            .Select(p => new ProductDto(p.Id, p.Name, p.Price, p.Stock, p.Description, p.Category))
            .ToListAsync();
    }

    public async Task<Product?> GetProductByIdAsync(int productId)
    {
        return await _context.Products.FindAsync(productId);
    }
}
