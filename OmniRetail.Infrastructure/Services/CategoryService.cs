using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using OmniRetail.Application.Interfaces;
using OmniRetail.Core.DTOs;
using OmniRetail.Core.Entities;
using OmniRetail.Infrastructure.Data;

namespace OmniRetail.Infrastructure.Services;

/// <summary>
/// Category service
/// SaaS-ready + production-oriented
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly OmniRetailDbContext _context;

    private readonly ILogger<CategoryService> _logger;

    public CategoryService(
        OmniRetailDbContext context,
        ILogger<CategoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    //
    // ========================================
    // GET ALL CATEGORIES
    // ========================================
    //
    public async Task<List<CategoryDto>> GetCategories()
    {
        return await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name
            })
            .ToListAsync();
    }

    //
    // ========================================
    // GET CATEGORY BY ID
    // ========================================
    //
    public async Task<CategoryDto?> GetCategoryById(Guid id)
    {
        if (id == Guid.Empty)
        {
            _logger.LogWarning(
                "Invalid category id received.");

            return null;
        }

        return await _context.Categories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name
            })
            .FirstOrDefaultAsync();
    }

    //
    // ========================================
    // CREATE CATEGORY
    // ========================================
    //
    public async Task<CategoryDto> CreateCategory(
        CreateCategoryRequest request)
    {
        if (request == null)
        {
            _logger.LogError(
                "CreateCategory request is null.");

            throw new ArgumentNullException(
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            _logger.LogWarning(
                "Category name is empty.");

            throw new ArgumentException(
                "Category name is required.");
        }

        //
        // SAFE NORMALIZATION
        //
        var normalizedName =
            request.Name.Trim();

        //
        // ANTI DUPLICATE
        //
        var exists = await _context.Categories
            .AnyAsync(c =>
                c.Name.ToLower() ==
                normalizedName.ToLower());

        if (exists)
        {
            _logger.LogWarning(
                "Category already exists: {Name}",
                normalizedName);

            throw new InvalidOperationException(
                "Category already exists.");
        }

        //
        // CREATE ENTITY
        //
        var category = new Category
        {
            Id = Guid.NewGuid(),

            Name = normalizedName
        };

        //
        // SAVE
        //
        _context.Categories.Add(category);

        await _context.SaveChangesAsync();

        //
        // LOGGING
        //
        _logger.LogInformation(
            "Category created successfully: {Name}",
            category.Name);

        //
        // RETURN DTO
        //
        return new CategoryDto
        {
            Id = category.Id,

            Name = category.Name
        };
    }

    //
    // ========================================
    // UPDATE CATEGORY
    // ========================================
    //
    public async Task<CategoryDto?> UpdateCategory(
        Guid id,
        CreateCategoryRequest request)
    {
        if (id == Guid.Empty)
        {
            _logger.LogWarning(
                "Invalid category id for update.");

            return null;
        }

        if (request == null)
        {
            throw new ArgumentNullException(
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException(
                "Category name is required.");
        }

        //
        // FIND CATEGORY
        //
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            _logger.LogWarning(
                "Category not found: {CategoryId}",
                id);

            return null;
        }

        //
        // NORMALIZATION
        //
        var normalizedName =
            request.Name.Trim();

        //
        // CHECK DUPLICATE
        //
        var exists = await _context.Categories
            .AnyAsync(c =>
                c.Id != id &&
                c.Name.ToLower() ==
                normalizedName.ToLower());

        if (exists)
        {
            _logger.LogWarning(
                "Update blocked. Category already exists: {Name}",
                normalizedName);

            throw new InvalidOperationException(
                "Category already exists.");
        }

        //
        // UPDATE
        //
        category.Name = normalizedName;

        await _context.SaveChangesAsync();

        //
        // LOGGING
        //
        _logger.LogInformation(
            "Category updated successfully: {CategoryId}",
            id);

        //
        // RETURN DTO
        //
        return new CategoryDto
        {
            Id = category.Id,

            Name = category.Name
        };
    }

    //
    // ========================================
    // DELETE CATEGORY
    // ========================================
    //
    public async Task<bool> DeleteCategory(Guid id)
    {
        if (id == Guid.Empty)
        {
            _logger.LogWarning(
                "Invalid category id for deletion.");

            return false;
        }

        //
        // FIND CATEGORY
        //
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            _logger.LogWarning(
                "Category not found: {CategoryId}",
                id);

            return false;
        }

        //
        // SAFETY CHECK
        // BLOCK DELETE IF PRODUCTS EXIST
        //
        var hasProducts = await _context.Products
            .AnyAsync(p =>
                p.CategoryId == id &&
                !p.IsDeleted);

        if (hasProducts)
        {
            _logger.LogWarning(
                "Delete blocked. Category contains products: {CategoryId}",
                id);

            throw new InvalidOperationException(
                "Cannot delete category linked to products.");
        }

        //
        // DELETE
        //
        _context.Categories.Remove(category);

        await _context.SaveChangesAsync();

        //
        // LOGGING
        //
        _logger.LogInformation(
            "Category deleted successfully: {CategoryId}",
            id);

        return true;
    }
}