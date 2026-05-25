using OmniRetail.Core.Enums;

namespace OmniRetail.Core.Entities;

/// <summary>
/// Représente une alerte système.
/// Exemple : stock faible, rupture, produit expirant...
/// </summary>
public class Alert
{
    public Guid Id { get; set; }

    /// <summary>Message de l'alerte.</summary>
    public string Message { get; set; } = string.Empty;

    // ========================================
    // BUG FIX #2 — TYPE SEVERITY : ENUM → STRING
    // ----------------------------------------
    // PROBLÈME ORIGINAL :
    //   Alert.Severity était de type AlertSeverity (enum = int 1-4)
    //
    // Cela causait DEUX erreurs simultanées :
    //
    //   (a) ERREUR DE COMPILATION dans InventoryService.cs :
    //       Severity = AlertType.StockLow.GetSeverity()
    //       GetSeverity() retourne string ("High"/"Medium"/"Low")
    //       mais AlertSeverity est un enum → assignation impossible.
    //
    //   (b) CONFLIT DE TYPE POSTGRESQL dans OmniRetailDbContext :
    //       e.Property(x => x.Severity).HasMaxLength(20).HasDefaultValue("Medium")
    //       EF Core stocke un enum en INTEGER par défaut.
    //       HasDefaultValue("Medium") est une string → type conflict au niveau
    //       de la migration PostgreSQL (colonne int, valeur par défaut string).
    //
    //   (c) INCOHÉRENCE avec AlertDto.Severity (string) et GetSeverity() (string).
    //       L'intention de conception était clairement de stocker "High"/"Medium"/"Low"
    //       comme string, pas comme int.
    //
    // CORRECTION : Severity devient string, cohérent avec toute la chaîne.
    // ========================================

    /// <summary>
    /// Niveau de gravité de l'alerte : "Low" | "Medium" | "High" | "Critical"
    /// Stocké en string pour cohérence avec AlertDto et GetSeverity().
    /// </summary>
    public string Severity { get; set; } = "Medium";

    /// <summary>Type d'alerte.</summary>
    public AlertType Type { get; set; }

    /// <summary>Indique si l'alerte a été lue.</summary>
    public bool IsRead { get; set; }

    /// <summary>Date de création de l'alerte.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Date de lecture de l'alerte (audit UI).</summary>
    public DateTimeOffset? ReadAt { get; set; }

    // ========================================
    // FOREIGN KEYS
    // ========================================

    /// <summary>Produit concerné par l'alerte.</summary>
    public Guid ProductId { get; set; }

    // ========================================
    // NAVIGATION PROPERTIES
    // ========================================

    /// <summary>Produit associé à l'alerte.</summary>
    public Product? Product { get; set; }
}
