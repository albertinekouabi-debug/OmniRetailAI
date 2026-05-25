using OmniRetail.Core.DTOs;

namespace OmniRetail.Application.Interfaces;

/// <summary>
/// Service de calcul des KPIs pour le dashboard principal
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Retourne tous les KPIs du dashboard en un seul appel
    /// </summary>
    Task<DashboardKpiDto> GetDashboardKpis();
}
