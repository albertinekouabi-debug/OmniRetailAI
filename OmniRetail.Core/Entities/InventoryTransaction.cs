using OmniRetail.Core.Enums;

namespace OmniRetail.Core.Entities;

/// <summary>
/// Historique des mouvements de stock.
/// </summary>
public class InventoryTransaction
{
    public Guid Id { get; set; }

    // Produit concerné
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    // Utilisateur ayant effectué l'action
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    // Type de transaction
    public InventoryTransactionType Type { get; set; }

    // Quantité modifiée
    public int Quantity { get; set; }

    // Ancien stock
    public int PreviousStock { get; set; }

    // Nouveau stock
    public int NewStock { get; set; }

    // Raison du mouvement
    public string? Reason { get; set; }

    // Date
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset Date { get; set; } = DateTimeOffset.UtcNow;
}