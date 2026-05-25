using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using OmniRetail.Application.Interfaces;

namespace OmniRetail.API.Controllers;

// ================================================================
// AUDIT CONTROLLER
// ================================================================

/// <summary>
/// Logs d'audit — Admin seulement
///
/// GET /api/audit          — Liste paginée des logs
/// GET /api/audit/user/{id} — Logs d'un utilisateur
/// </summary>
[ApiController]
[Route("api/audit")]
[Authorize(Roles = "Admin")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _audit;

    public AuditController(IAuditService audit) => _audit = audit;

    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int      page     = 1,
        [FromQuery] int      pageSize = 50,
        [FromQuery] string?  action   = null,
        [FromQuery] Guid?    userId   = null,
        [FromQuery] DateTime? from    = null,
        [FromQuery] DateTime? to      = null,
        CancellationToken ct = default)
    {
        var result = await _audit.GetLogsAsync(
            page, pageSize, action, userId, from, to, ct);

        return Ok(result);
    }

    [HttpGet("user/{id:guid}")]
    public async Task<IActionResult> GetUserLogs(
        Guid id,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var result = await _audit.GetLogsAsync(
            page: 1, pageSize: limit, userId: id, ct: ct);

        return Ok(result);
    }
}

// ================================================================
// REPORTS CONTROLLER
// ================================================================

/// <summary>
/// Rapports analytiques avancés
///
/// GET /api/reports/sales  — Rapport ventes sur période
/// GET /api/reports/stock  — Rapport état du stock
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService  _reports;
    private readonly IAuditService   _audit;

    public ReportsController(
        IReportService reports,
        IAuditService audit)
    {
        _reports = reports;
        _audit   = audit;
    }

    /// <summary>
    /// Rapport des ventes sur une période donnée.
    /// Inclut : CA total, ventes quotidiennes, top produits, revenus par catégorie.
    /// </summary>
    [HttpGet("sales")]
    public async Task<IActionResult> GetSalesReport(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to   = null,
        CancellationToken ct = default)
    {
        var start = from ?? DateTime.UtcNow.AddDays(-30);
        var end   = to   ?? DateTime.UtcNow;

        if (start > end)
            return BadRequest(new { message = "La date de début doit être antérieure à la date de fin." });

        var report = await _reports.GetSalesReportAsync(start, end, ct);

        await _audit.LogAsync(
            "ReportSales",
            GetUserId(), GetUsername(),
            additionalInfo: $"{start:yyyy-MM-dd} → {end:yyyy-MM-dd}",
            ct: ct);

        return Ok(report);
    }

    /// <summary>
    /// Rapport de l'état du stock : produits critiques, expirants, valeur totale.
    /// </summary>
    [HttpGet("stock")]
    public async Task<IActionResult> GetStockReport(CancellationToken ct = default)
    {
        var report = await _reports.GetStockReportAsync(ct);
        return Ok(report);
    }

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
}
