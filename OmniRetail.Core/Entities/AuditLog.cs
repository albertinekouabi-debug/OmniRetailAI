namespace OmniRetail.Core.Entities;

/// <summary>
/// Audit Log — Traçabilité immuable de toutes les opérations critiques.
///
/// Règle : les AuditLog ne sont jamais modifiés ni supprimés via l'API.
/// Rétention : archivage après 1 an recommandé.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // ── Qui ──────────────────────────────────────────────────
    public Guid?   UserId   { get; set; }
    public string? Username { get; set; }

    // ── Quoi ─────────────────────────────────────────────────
    /// <summary>ex: "Login", "CreateProduct", "CreateSale", "DeleteUser"</summary>
    public string  Action     { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId   { get; set; }
    public string? OldValues  { get; set; }
    public string? NewValues  { get; set; }
    public string? AdditionalInfo { get; set; }

    // ── Contexte réseau ───────────────────────────────────────
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // ── Résultat ─────────────────────────────────────────────
    public bool    IsSuccess     { get; set; } = true;
    public string? ErrorMessage  { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
