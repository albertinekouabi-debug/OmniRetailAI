using OmniRetail.Core.DTOs;

namespace OmniRetail.Application.Interfaces;

/// <summary>
/// Service POS (Point Of Sale) - Gestion des ventes
/// </summary>
public interface ISaleService
{
    // ========================================
    // VENTES
    // ========================================

    /// <summary>
    /// Crée une vente, décrémente le stock, enregistre les transactions d'inventaire.
    /// Opération transactionnelle complète.
    /// </summary>
    Task<SaleDto> CreateSale(Guid userId, CreateSaleRequest request);

    /// <summary>
    /// Historique des ventes (pagination simple)
    /// </summary>
    Task<List<SaleDto>> GetSales(int page = 1, int pageSize = 50);

    /// <summary>
    /// Détail d'une vente par Id
    /// </summary>
    Task<SaleDto?> GetSaleById(Guid id);

    /// <summary>
    /// Ventes du jour
    /// </summary>
    Task<List<SaleDto>> GetTodaySales();
}
