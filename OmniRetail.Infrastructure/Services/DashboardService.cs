using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using OmniRetail.Application.Interfaces;
using OmniRetail.Core.DTOs;
using OmniRetail.Infrastructure.Data;

namespace OmniRetail.Infrastructure.Services;

/// <summary>
/// Dashboard KPI Service
/// Calcule les indicateurs clés de performance en temps réel
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly OmniRetailDbContext _context;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        OmniRetailDbContext context,
        ILogger<DashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    //
    // ========================================
    // GET DASHBOARD KPIS
    // ========================================
    //
    public async Task<DashboardKpiDto> GetDashboardKpis()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var tomorrow = today.AddDays(1);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var expirationThreshold = today.AddDays(7);

        //
        // VENTES DU JOUR
        //
        var todaySales = await _context.Sales
            .AsNoTracking()
            .Where(s => s.CreatedAt >= today && s.CreatedAt < tomorrow)
            .ToListAsync();

        var todayRevenue = todaySales.Sum(s => s.TotalAmount);
        var todaySalesCount = todaySales.Count;

        //
        // VENTES DU MOIS
        //
        var monthSales = await _context.Sales
            .AsNoTracking()
            .Where(s => s.CreatedAt >= monthStart)
            .ToListAsync();

        var monthRevenue = monthSales.Sum(s => s.TotalAmount);
        var monthSalesCount = monthSales.Count;

        //
        // PRODUITS
        //
        var totalProducts = await _context.Products
            .AsNoTracking()
            .CountAsync(p => !p.IsDeleted);

        var criticalStockCount = await _context.Products
            .AsNoTracking()
            .CountAsync(p =>
                !p.IsDeleted &&
                p.CurrentStock <= p.CriticalStock);

        var expiringProductsCount = await _context.Products
            .AsNoTracking()
            .CountAsync(p =>
                !p.IsDeleted &&
                p.ExpirationDate.HasValue &&
                p.ExpirationDate.Value.Date <= expirationThreshold);

        //
        // ALERTES
        //
        var unreadAlertsCount = await _context.Alerts
            .AsNoTracking()
            .CountAsync(a => !a.IsRead);

        //
        // PRODUITS CRITIQUES (liste)
        //
        var criticalProducts = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p =>
                !p.IsDeleted &&
                p.CurrentStock <= p.CriticalStock)
            .OrderBy(p => p.CurrentStock)
            .Take(10)
            .Select(p => new CriticalProductDto
            {
                Id = p.Id,
                Name = p.Name,
                CategoryName = p.Category != null ? p.Category.Name : string.Empty,
                CurrentStock = p.CurrentStock,
                CriticalStock = p.CriticalStock,
                ExpirationDate = p.ExpirationDate
            })
            .ToListAsync();

        //
        // ALERTES RÉCENTES
        //
        var recentAlerts = await _context.Alerts
            .AsNoTracking()
            .Include(a => a.Product)
            .Where(a => !a.IsRead)
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .Select(a => new AlertDto
            {
                Id = a.Id,
                Type = a.Type.ToString(),
                ProductId = a.ProductId,
                ProductName = a.Product != null ? a.Product.Name : string.Empty,
                Message = a.Message,
                Severity = a.Severity,
                CreatedAt = a.CreatedAt,
                IsRead = a.IsRead,
                ReadAt = a.ReadAt
            })
            .ToListAsync();

        //
        // TOP 5 PRODUITS DU MOIS
        //
        var topProducts = await _context.SaleItems
            .AsNoTracking()
            .Include(si => si.Sale)
            .Where(si => si.Sale.CreatedAt >= monthStart)
            .GroupBy(si => new { si.ProductId, si.ProductName })
            .Select(g => new TopProductDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                TotalQuantitySold = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.TotalPrice)
            })
            .OrderByDescending(x => x.TotalRevenue)
            .Take(5)
            .ToListAsync();

        _logger.LogInformation(
            "Dashboard KPIs computed. Revenue today: {Today}, month: {Month}",
            todayRevenue, monthRevenue);

        return new DashboardKpiDto
        {
            TodayRevenue = todayRevenue,
            MonthRevenue = monthRevenue,
            TodaySalesCount = todaySalesCount,
            MonthSalesCount = monthSalesCount,
            TotalProducts = totalProducts,
            CriticalStockCount = criticalStockCount,
            ExpiringProductsCount = expiringProductsCount,
            UnreadAlertsCount = unreadAlertsCount,
            CriticalProducts = criticalProducts,
            RecentAlerts = recentAlerts,
            TopProducts = topProducts
        };
    }
}
