using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using OmniRetail.Application.Interfaces;
using OmniRetail.Core.DTOs;

namespace OmniRetail.API.Controllers;

/// <summary>
/// Assistant IA OmniRetail
///
/// POST /api/ai/query — Question à l'assistant (données métier temps réel)
/// GET  /api/ai/suggestions — Suggestions de questions contextuelles
/// </summary>
[ApiController]
[Route("api/ai")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IAiAssistantService _ai;
    private readonly IAuditService       _audit;
    private readonly ILogger<AiController> _logger;

    public AiController(
        IAiAssistantService ai,
        IAuditService audit,
        ILogger<AiController> logger)
    {
        _ai    = ai;
        _audit = audit;
        _logger = logger;
    }

    // ============================================================
    // POST /api/ai/query
    // ============================================================

    /// <summary>
    /// Pose une question à l'assistant IA OmniRetail.
    /// L'assistant a accès aux données métier temps réel.
    /// </summary>
    [HttpPost("query")]
    [EnableRateLimiting("ai")]
    public async Task<IActionResult> Query(
        [FromBody] AiQueryRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId   = GetUserId();
        var username = GetUsername() ?? "unknown";
        var role     = GetRole() ?? "Employee";

        _logger.LogInformation(
            "AI query from {Username} ({Role}): {Question}",
            username, role, request.Question);

        var response = await _ai.QueryAsync(request, userId, username, role, ct);

        await _audit.LogAsync(
            "AiQuery",
            userId, username,
            "AI", null,
            additionalInfo: request.Question[..Math.Min(100, request.Question.Length)],
            isSuccess:      response.IsSuccess,
            ipAddress:      GetIp(),
            ct:             ct);

        return Ok(response);
    }

    // ============================================================
    // GET /api/ai/suggestions
    // ============================================================

    /// <summary>
    /// Retourne des suggestions de questions contextuelles selon le rôle.
    /// </summary>
    [HttpGet("suggestions")]
    public IActionResult GetSuggestions()
    {
        var role = GetRole() ?? "Employee";
        var isAdmin = role.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        var suggestions = isAdmin
            ? AdminSuggestions
            : EmployeeSuggestions;

        return Ok(new { suggestions });
    }

    // ── Suggestions ─────────────────────────────────────────────

    private static readonly string[] AdminSuggestions =
    [
        "Quels sont les 5 produits les plus rentables ce mois-ci ?",
        "Quels produits sont en rupture de stock ?",
        "Quel est le chiffre d'affaires d'aujourd'hui ?",
        "Quels produits expirent dans les 7 prochains jours ?",
        "Quels sont les produits les moins vendus ?",
        "Analyse des ventes par catégorie ce mois",
        "Prévision des ventes pour le mois prochain",
        "Quels employés réalisent le plus de ventes ?",
        "Quels produits devraient être réapprovisionnés en priorité ?",
        "Afficher les tendances des ventes des 30 derniers jours"
    ];

    private static readonly string[] EmployeeSuggestions =
    [
        "Quel stock reste-t-il pour ce produit ?",
        "Quels produits sont en stock critique ?",
        "Quels produits expirent bientôt ?",
        "Comment puis-je enregistrer une entrée de stock ?",
        "Comment créer une nouvelle vente ?",
        "Quelles alertes sont actives aujourd'hui ?"
    ];

    // ── Helpers ─────────────────────────────────────────────────

    private Guid GetUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    private string? GetUsername()
        => User.FindFirst(ClaimTypes.Name)?.Value
        ?? User.FindFirst("unique_name")?.Value;

    private string? GetRole()
        => User.FindFirst(ClaimTypes.Role)?.Value;

    private string? GetIp()
    {
        var fwd = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        return !string.IsNullOrWhiteSpace(fwd)
            ? fwd.Split(',').First().Trim()
            : HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
