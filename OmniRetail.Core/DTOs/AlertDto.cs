namespace OmniRetail.Core.DTOs;

/// <summary>
/// DTO de lecture des alertes système
/// Utilisé pour dashboard, notifications et monitoring
/// </summary>
public class AlertDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// Type d’alerte (StockLow, Expiring, etc.)
    /// Stocké en string pour compatibilité API/UI
    /// </summary>
    public string Type { get; set; } = string.Empty;

    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Niveau de gravité (Low, Medium, High, Critical)
    /// </summary>
    public string Severity { get; set; } = "Medium";

    public DateTimeOffset CreatedAt { get; set; }

    public bool IsRead { get; set; }

    /// <summary>
    /// Date de lecture (audit UI)
    /// </summary>
    public DateTimeOffset? ReadAt { get; set; }
}