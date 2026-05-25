using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using OmniRetail.Application.Interfaces;
using OmniRetail.Core.DTOs;

namespace OmniRetail.API.Controllers;

/// <summary>
/// Gestion des mouvements de stock (entrées, sorties, ajustements)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(
        IInventoryService inventoryService,
        ILogger<InventoryController> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    //
    // GET: api/inventory/transactions
    //
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions()
    {
        var transactions = await _inventoryService.GetTransactions();
        return Ok(transactions);
    }

    //
    // POST: api/inventory/transactions
    // Entrée ou sortie de stock
    //
    [HttpPost("transactions")]
    public async Task<IActionResult> AddTransaction(
        [FromBody] CreateInventoryTransactionRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();

        try
        {
            var result = await _inventoryService
                .AddTransaction(userId, request);

            return CreatedAtAction(
                nameof(GetTransactions),
                null,
                result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid inventory operation.");
            return BadRequest(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    //
    // POST: api/inventory/adjust
    // Ajustement manuel (Admin seulement)
    //
    [HttpPost("adjust")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdjustStock(
        [FromBody] AdjustmentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();

        try
        {
            var result = await _inventoryService
                .AdjustStock(userId, request);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid stock adjustment.");
            return BadRequest(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    //
    // GET: api/inventory/alerts
    //
    [HttpGet("alerts")]
    public async Task<IActionResult> GetAlerts(
        [FromQuery] bool unreadOnly = false)
    {
        var alerts = await _inventoryService.GetAlerts(unreadOnly);
        return Ok(alerts);
    }

    //
    // GET: api/inventory/alerts/count
    //
    [HttpGet("alerts/count")]
    public async Task<IActionResult> GetUnreadAlertsCount()
    {
        var count = await _inventoryService.GetUnreadAlertsCount();
        return Ok(new { count });
    }

    //
    // GET: api/inventory/alerts/product/{productId}
    //
    [HttpGet("alerts/product/{productId:guid}")]
    public async Task<IActionResult> GetAlertsByProduct(Guid productId)
    {
        var alerts = await _inventoryService.GetAlertsByProduct(productId);
        return Ok(alerts);
    }

    //
    // PATCH: api/inventory/alerts/{id}/read
    //
    [HttpPatch("alerts/{id:guid}/read")]
    public async Task<IActionResult> MarkAlertAsRead(Guid id)
    {
        await _inventoryService.MarkAlertAsRead(id);
        return NoContent();
    }

    //
    // PATCH: api/inventory/alerts/read-all
    //
    [HttpPatch("alerts/read-all")]
    public async Task<IActionResult> MarkAllAlertsAsRead()
    {
        await _inventoryService.MarkAllAlertsAsRead();
        return NoContent();
    }

    //
    // POST: api/inventory/alerts/check
    // Force le recalcul des alertes (Admin)
    //
    [HttpPost("alerts/check")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CheckAlerts()
    {
        await _inventoryService.CheckAndCreateAlerts();
        return Ok(new { message = "Alertes recalculées avec succès." });
    }

    //
    // DELETE: api/inventory/alerts/cleanup
    // Nettoyage des alertes anciennes (Admin)
    //
    [HttpDelete("alerts/cleanup")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CleanupAlerts(
        [FromQuery] int days = 30)
    {
        await _inventoryService.CleanupOldAlerts(days);
        return Ok(new { message = $"Alertes de plus de {days} jours supprimées." });
    }

    // ========================================
    // HELPER
    // ========================================

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
