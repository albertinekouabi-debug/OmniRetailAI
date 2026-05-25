using System.ComponentModel.DataAnnotations;

namespace OmniRetail.Core.DTOs;

public class UpdateProductRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public Guid CategoryId { get; set; }

    [Required]
    [Range(typeof(decimal), "0,01", "999999999")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int CurrentStock { get; set; }

    [Range(0, int.MaxValue)]
    public int CriticalStock { get; set; }

    public DateTime? ExpirationDate { get; set; }

    public bool IsSensitive { get; set; }
}