using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using OmniRetail.Application.Interfaces;

namespace OmniRetail.API.Controllers;

/// <summary>
/// Dashboard — KPIs et statistiques
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardService dashboardService,
        ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    //
    // GET: api/dashboard/kpis
    // Retourne tous les KPIs en un seul appel
    //
    [HttpGet("kpis")]
    public async Task<IActionResult> GetKpis()
    {
        try
        {
            var kpis = await _dashboardService.GetDashboardKpis();
            return Ok(kpis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard KPIs.");
            return StatusCode(500, new { error = "Erreur lors du calcul des KPIs." });
        }
    }
}
