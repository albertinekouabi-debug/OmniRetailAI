using System.Collections.Generic;

namespace OmniRetail.Core.Entities;

/// <summary>
/// Produit principal du système OmniRetail.
/// Compatible EF Core + Soft Delete + PostgreSQL.
/// </summary>
public class Product : BaseEntity
{
    // ========================================
    // INFORMATIONS PRINCIPALES
    // ========================================

    /// <summary>Nom du produit.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Description détaillée.</summary>
    public string? Description { get; set; }

    /// <summary>Prix unitaire.</summary>
    public decimal Price { get; set; }

    /// <summary>Quantité actuelle en stock.</summary>
    public int CurrentStock { get; set; }

    /// <summary>Seuil critique minimal.</summary>
    public int CriticalStock { get; set; } = 5;

    /// <summary>Produit sensible.</summary>
    public bool IsSensitive { get; set; } = false;

    /// <summary>Date d'expiration.</summary>
    public DateTime? ExpirationDate { get; set; }

    // ========================================
    // SOFT DELETE
    // ========================================

    /// <summary>Suppression logique.</summary>
    public bool IsDeleted { get; set; } = false;

    // ========================================
    // FOREIGN KEYS
    // ========================================

    /// <summary>Catégorie associée.</summary>
    public Guid CategoryId { get; set; }

    // ========================================
    // NAVIGATION PROPERTIES
    // ========================================

    /// <summary>Catégorie du produit.</summary>
    public Category? Category { get; set; }

    /// <summary>Alertes liées au produit.</summary>
    public ICollection<Alert> Alerts { get; set; }
        = new List<Alert>();

    /// <summary>Historique des mouvements de stock.</summary>
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; }
        = new List<InventoryTransaction>();

    /// <summary>Ventes contenant ce produit.</summary>
    public ICollection<SaleItem> SaleItems { get; set; }
        = new List<SaleItem>();
}