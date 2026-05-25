namespace OmniRetail.Core.Entities;

/// <summary>
/// Représente une catégorie de produits.
/// Exemple : Électronique, Boissons, Alimentaire...
/// </summary>
public class Category
{
    public Guid Id { get; set; }

    /// <summary>
    /// Nom unique de la catégorie.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description optionnelle.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Date de création.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // ========================================
    // NAVIGATION PROPERTIES
    // ========================================

    /// <summary>
    /// Liste des produits appartenant
    /// à cette catégorie.
    /// </summary>
    public ICollection<Product> Products { get; set; }
        = new List<Product>();
}