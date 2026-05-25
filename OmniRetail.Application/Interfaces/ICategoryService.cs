using OmniRetail.Core.DTOs;

namespace OmniRetail.Application.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetCategories();
    Task<CategoryDto?> GetCategoryById(Guid id);
    Task<CategoryDto> CreateCategory(CreateCategoryRequest request);
    Task<bool> DeleteCategory(Guid id);
}