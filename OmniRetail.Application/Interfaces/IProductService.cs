using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OmniRetail.Core.DTOs;

namespace OmniRetail.Application.Interfaces;

public interface IProductService
{
    Task<List<ProductDto>> GetAllProducts(
        string? search,
        Guid? categoryId);

    Task<ProductDto?> GetProductById(Guid id);

    Task<ProductDto> CreateProduct(
        CreateProductRequest request);

    Task<ProductDto?> UpdateProduct(
        Guid id,
        UpdateProductRequest request);

    Task DeleteProduct(Guid id);

    Task<List<CategoryDto>> GetCategories();

    Task<CategoryDto> CreateCategory(
        CreateCategoryRequest request);

    Task<CategoryDto?> GetCategoryById(Guid id);
}