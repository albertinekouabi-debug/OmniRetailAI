using System.ComponentModel.DataAnnotations;

namespace OmniRetail.Core.DTOs;

/// <summary>
/// Request DTO for creating a category
/// </summary>
public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
}