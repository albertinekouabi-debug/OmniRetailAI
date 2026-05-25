namespace OmniRetail.Core.Entities;

/// <summary>
/// Journal des interactions IA
/// Stocke les requêtes, prédictions et insights générés par le module IA
/// </summary>
public class AILog : BaseEntity
{
    /// <summary>
    /// Type d'opération IA (Prediction, Forecast, Insight, etc.)
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Input de la requête IA (sérialisé en JSON si nécessaire)
    /// </summary>
    public string? Input { get; set; }

    /// <summary>
    /// Résultat de l'opération IA
    /// </summary>
    public string? Output { get; set; }

    /// <summary>
    /// Durée de traitement en ms (performance monitoring)
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Utilisateur ayant déclenché l'opération IA
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Succès ou échec
    /// </summary>
    public bool IsSuccess { get; set; } = true;

    /// <summary>
    /// Message d'erreur si IsSuccess = false
    /// </summary>
    public string? ErrorMessage { get; set; }
}
