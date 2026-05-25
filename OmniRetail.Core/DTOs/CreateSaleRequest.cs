using System.ComponentModel.DataAnnotations;

namespace OmniRetail.Core.DTOs;

/// <summary>
/// Requête de création de vente POS
/// Une vente = un ou plusieurs articles vendus en une transaction
/// </summary>
public class CreateSaleRequest
{
    /// <summary>
    /// Liste des articles de la vente (obligatoire, min 1 article)
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "La vente doit contenir au moins un article.")]
    public List<CreateSaleItemRequest> Items { get; set; } = new();

    /// <summary>
    /// Note optionnelle (ex : numéro de caisse, client, etc.)
    /// </summary>
    [MaxLength(500)]
    public string? Note { get; set; }
}

/// <summary>
/// Article d'une vente POS
/// </summary>
public class CreateSaleItemRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "La quantité doit être supérieure à zéro.")]
    public int Quantity { get; set; }
}
