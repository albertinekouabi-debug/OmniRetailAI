using System.ComponentModel.DataAnnotations;
using OmniRetail.Core.Enums;

namespace OmniRetail.Core.DTOs;

/// <summary>
/// Requête création mouvement stock
/// </summary>
public class CreateInventoryTransactionRequest
{
    [Required]
    public Guid ProductId { get; set; }

    /// <summary>
    /// Toujours positive côté API
    /// Le service convertit en négatif si Out
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    public InventoryTransactionType Type { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }
}