namespace OmniRetail.Core.Enums;

/// <summary>
/// Types d'alertes système pour la gestion intelligente du stock
/// Utilisé pour notifier les risques opérationnels
/// </summary>
public enum AlertType
{
    /// <summary>
    /// Stock critique (inférieur au seuil défini)
    /// </summary>
    StockLow = 1,

    /// <summary>
    /// Produit proche de la date d'expiration
    /// </summary>
    Expiring = 2
}

/// <summary>
/// Extensions pour enrichir les alertes sans casser l'architecture
/// </summary>
public static class AlertTypeExtensions
{
    public static string ToFriendlyName(this AlertType type)
    {
        return type switch
        {
            AlertType.StockLow => "Stock faible",
            AlertType.Expiring => "Produit expirant",
            _ => "Alerte inconnue"
        };
    }

    public static string GetSeverity(this AlertType type)
    {
        return type switch
        {
            AlertType.StockLow => "High",
            AlertType.Expiring => "Medium",
            _ => "Low"
        };
    }
}