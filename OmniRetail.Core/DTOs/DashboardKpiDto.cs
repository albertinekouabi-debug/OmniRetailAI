namespace OmniRetail.Core.DTOs;

/// <summary>
/// KPIs du dashboard principal OmniRetail
/// Données temps réel pour la vue directeur/admin
/// </summary>
public class DashboardKpiDto
{
    // ========================================
    // VENTES
    // ========================================

    /// <summary>Chiffre d'affaires du jour</summary>
    public decimal TodayRevenue { get; set; }

    /// <summary>Chiffre d'affaires du mois en cours</summary>
    public decimal MonthRevenue { get; set; }

    /// <summary>Nombre de ventes du jour</summary>
    public int TodaySalesCount { get; set; }

    /// <summary>Nombre de ventes du mois</summary>
    public int MonthSalesCount { get; set; }

    // ========================================
    // STOCK
    // ========================================

    /// <summary>Total produits actifs</summary>
    public int TotalProducts { get; set; }

    /// <summary>Produits en stock critique</summary>
    public int CriticalStockCount { get; set; }

    /// <summary>Produits expirant dans les 7 jours</summary>
    public int ExpiringProductsCount { get; set; }

    // ========================================
    // ALERTES
    // ========================================

    /// <summary>Alertes non lues</summary>
    public int UnreadAlertsCount { get; set; }

    // ========================================
    // PRODUITS CRITIQUES
    // ========================================

    /// <summary>Liste des produits avec stock critique</summary>
    public List<CriticalProductDto> CriticalProducts { get; set; } = new();

    /// <summary>Alertes récentes (les 10 dernières)</summary>
    public List<AlertDto> RecentAlerts { get; set; } = new();

    /// <summary>Top 5 produits les plus vendus ce mois</summary>
    public List<TopProductDto> TopProducts { get; set; } = new();
}

/// <summary>
/// Produit en alerte stock critique
/// </summary>
public class CriticalProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int CriticalStock { get; set; }
    public DateTime? ExpirationDate { get; set; }
}

/// <summary>
/// Top produit vendu ce mois
/// </summary>
public class TopProductDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int TotalQuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
}
