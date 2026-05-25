using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

using OmniRetail.Application.Interfaces;
using OmniRetail.Core.DTOs;
using OmniRetail.Core.Entities;
using OmniRetail.Infrastructure.Data;

using System.Text.Json;

namespace OmniRetail.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly OmniRetailDbContext _context;
    private readonly IDistributedCache _cache;

    public ProductService(
        OmniRetailDbContext context,
        IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    //
    // GET ALL PRODUCTS WITH REDIS CACHE
    //
    public async Task<List<ProductDto>> GetAllProducts(
        string? search,
        Guid? categoryId)
    {
        var cacheKey =
            $"products_{search}_{categoryId}";

        var cachedProducts =
            await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedProducts))
        {
            return JsonSerializer.Deserialize<List<ProductDto>>(
                cachedProducts)!;
        }

        var query = _context.Products
            .Include(x => x.Category)
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.Name.ToLower()
                    .Contains(search.ToLower()));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(x =>
                x.CategoryId == categoryId.Value);
        }

        var products = await query
            .Select(x => new ProductDto
            {
                Id = x.Id,
                Name = x.Name,
                CategoryId = x.CategoryId,
                CategoryName = x.Category.Name,
                Price = x.Price,
                CurrentStock = x.CurrentStock,
                CriticalStock = x.CriticalStock,
                ExpirationDate = x.ExpirationDate,
                IsSensitive = x.IsSensitive
            })
            .ToListAsync();

        var options =
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow =
                    TimeSpan.FromMinutes(5)
            };

        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(products),
            options);

        return products;
    }

    //
    // GET PRODUCT BY ID
    //
    public async Task<ProductDto?> GetProductById(Guid id)
    {
        var product = await _context.Products
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                !x.IsDeleted);

        if (product == null)
            return null;

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            CategoryId = product.CategoryId,
            CategoryName = product.Category.Name,
            Price = product.Price,
            CurrentStock = product.CurrentStock,
            CriticalStock = product.CriticalStock,
            ExpirationDate = product.ExpirationDate,
            IsSensitive = product.IsSensitive
        };
    }

    //
    // CREATE PRODUCT
    //
    public async Task<ProductDto> CreateProduct(
        CreateProductRequest request)
    {
        var product = new Product
        {
            Name = request.Name,
            CategoryId = request.CategoryId,
            Price = request.Price,
            CurrentStock = request.CurrentStock,
            CriticalStock = request.CriticalStock,
            ExpirationDate = request.ExpirationDate,
            IsSensitive = request.IsSensitive
        };

        _context.Products.Add(product);

        await _context.SaveChangesAsync();

        //
        // CLEAR CACHE
        //
        await _cache.RemoveAsync(
            $"products__");

        return await GetProductById(product.Id)
            ?? throw new Exception(
                "Product creation failed.");
    }

    //
    // UPDATE PRODUCT
    //
    public async Task<ProductDto?> UpdateProduct(
        Guid id,
        UpdateProductRequest request)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(x =>
                x.Id == id);

        if (product == null)
            return null;

        product.Name = request.Name;
        product.CategoryId = request.CategoryId;
        product.Price = request.Price;
        product.CurrentStock = request.CurrentStock;
        product.CriticalStock = request.CriticalStock;
        product.ExpirationDate = request.ExpirationDate;
        product.IsSensitive = request.IsSensitive;

        await _context.SaveChangesAsync();

        //
        // CLEAR CACHE
        //
        await _cache.RemoveAsync(
            $"products__");

        return await GetProductById(product.Id);
    }

    //
    // DELETE PRODUCT (SOFT DELETE)
    //
    public async Task DeleteProduct(Guid id)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(x =>
                x.Id == id);

        if (product == null)
            return;

        product.IsDeleted = true;

        await _context.SaveChangesAsync();

        //
        // CLEAR CACHE
        //
        await _cache.RemoveAsync(
            $"products__");
    }

    //
    // GET ALL CATEGORIES
    //
    public async Task<List<CategoryDto>> GetCategories()
    {
        return await _context.Categories
            .Select(x => new CategoryDto
            {
                Id = x.Id,
                Name = x.Name
            })
            .ToListAsync();
    }

    //
    // GET CATEGORY BY ID
    //
    public async Task<CategoryDto?> GetCategoryById(
        Guid id)
    {
        return await _context.Categories
            .Where(x => x.Id == id)
            .Select(x => new CategoryDto
            {
                Id = x.Id,
                Name = x.Name
            })
            .FirstOrDefaultAsync();
    }

    //
    // CREATE CATEGORY
    //
    public async Task<CategoryDto> CreateCategory(
        CreateCategoryRequest request)
    {
        var exists = await _context.Categories
            .AnyAsync(x =>
                x.Name.ToLower() ==
                request.Name.ToLower());

        if (exists)
        {
            throw new Exception(
                "Category already exists.");
        }

        var category = new Category
        {
            Name = request.Name
        };

        _context.Categories.Add(category);

        await _context.SaveChangesAsync();

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name
        };
    }
}