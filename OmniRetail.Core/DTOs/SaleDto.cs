namespace OmniRetail.Core.DTOs;

/// <summary>
/// DTO de lecture d'une vente POS
/// Utilisé pour : API, historique, dashboard, ticket caisse
/// </summary>
public class SaleDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// Utilisateur / caissier ayant effectué la vente
    /// </summary>
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Montant total de la vente (TTC)
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Date/heure de la vente
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Articles vendus
    /// </summary>
    public List<SaleItemDto> Items { get; set; } = new();
}

/// <summary>
/// DTO d'un article de vente
/// </summary>
public class SaleItemDto
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    /// <summary>
    /// Snapshot nom produit au moment de la vente
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }
}
