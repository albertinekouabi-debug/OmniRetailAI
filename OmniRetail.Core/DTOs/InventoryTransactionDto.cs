namespace OmniRetail.Core.DTOs;

/// <summary>
/// DTO de lecture des mouvements de stock
/// Utilisé pour :
/// - API
/// - dashboard
/// - historique
/// - audit
/// - reporting
/// </summary>
public class InventoryTransactionDto
{
    /// <summary>
    /// Identifiant transaction
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Produit concerné
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Nom produit snapshot
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Quantité mouvementée
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Type :
    /// In / Out / Adjustment
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Date transaction
    /// </summary>
    public DateTimeOffset Date { get; set; }

    /// <summary>
    /// Utilisateur ayant effectué l'action
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Raison métier
    /// </summary>
    public string? Reason { get; set; }

    // ========================================
    // SNAPSHOT STOCK
    // ========================================

    /// <summary>
    /// Stock avant mouvement
    /// </summary>
    public int PreviousStock { get; set; }

    /// <summary>
    /// Stock après mouvement
    /// </summary>
    public int NewStock { get; set; }
}