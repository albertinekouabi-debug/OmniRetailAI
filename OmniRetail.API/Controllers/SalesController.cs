using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using OmniRetail.Application.Interfaces;
using OmniRetail.Core.DTOs;

namespace OmniRetail.API.Controllers;

/// <summary>
/// POS — Point Of Sale
/// Création et consultation des ventes
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SalesController : ControllerBase
{
    private readonly ISaleService _saleService;
    private readonly ILogger<SalesController> _logger;

    public SalesController(
        ISaleService saleService,
        ILogger<SalesController> logger)
    {
        _saleService = saleService;
        _logger = logger;
    }

    //
    // GET: api/sales
    // Historique des ventes avec pagination
    //
    [HttpGet]
    public async Task<IActionResult> GetSales(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var sales = await _saleService.GetSales(page, pageSize);
        return Ok(sales);
    }

    //
    // GET: api/sales/{id}
    //
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var sale = await _saleService.GetSaleById(id);

        if (sale == null)
            return NotFound(new { error = "Vente introuvable." });

        return Ok(sale);
    }

    //
    // GET: api/sales/today
    // Ventes du jour
    //
    [HttpGet("today")]
    public async Task<IActionResult> GetTodaySales()
    {
        var sales = await _saleService.GetTodaySales();
        return Ok(sales);
    }

    //
    // POST: api/sales
    // Création d'une vente POS — décrément stock automatique
    //
    [HttpPost]
    public async Task<IActionResult> CreateSale(
        [FromBody] CreateSaleRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();

        if (userId == Guid.Empty)
            return Unauthorized(new { error = "Utilisateur non identifié." });

        try
        {
            var sale = await _saleService.CreateSale(userId, request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = sale.Id },
                sale);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "POS sale creation failed.");
            return BadRequest(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
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
