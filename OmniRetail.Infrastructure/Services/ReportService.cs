using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using OmniRetail.Application.Interfaces;
using OmniRetail.Core.DTOs;
using OmniRetail.Infrastructure.Data;

namespace OmniRetail.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly OmniRetailDbContext    _context;
    private readonly ICacheService          _cache;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        OmniRetailDbContext context,
        ICacheService cache,
        ILogger<ReportService> logger)
    {
        _context = context;
        _cache   = cache;
        _logger  = logger;
    }

    // ============================================================
    // SALES REPORT
    // ============================================================

    public async Task<SalesReportDto> GetSalesReportAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
    {
        var cacheKey = $"report:sales:{from:yyyyMMdd}:{to:yyyyMMdd}";

        return await _cache.GetOrCreateAsync(cacheKey, async (t) =>
        {
            var sales = await _context.Sales
                .AsNoTracking()
                .Include(s => s.Items)
                .Where(s => s.CreatedAt >= from && s.CreatedAt <= to)
                .ToListAsync(t);

            var totalRevenue = sales.Sum(s => s.TotalAmount);
            var totalSales   = sales.Count;

            // Ventes quotidiennes
            var dailySales = sales
                .GroupBy(s => s.CreatedAt.Date)
                .Select(g => new DailySalesDto
                {
                    Date    = g.Key,
                    Revenue = g.Sum(x => x.TotalAmount),
                    Count   = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToList();

            // Top produits
            var topProducts = sales
                .SelectMany(s => s.Items)
                .GroupBy(i => new { i.ProductId, i.ProductName })
                .Select(g => new TopProductDto
                {
                    ProductId       = g.Key.ProductId,
                    ProductName     = g.Key.ProductName,
                    TotalQuantitySold = g.Sum(x => x.Quantity),
                    TotalRevenue    = g.Sum(x => x.TotalPrice)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(10)
                .ToList();

            // Revenue par catégorie
            var allItems = sales.SelectMany(s => s.Items).ToList();

            var productIds    = allItems.Select(i => i.ProductId).Distinct().ToList();
            var productCats   = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Category?.Name ?? "N/A", t);

            var byCategory = allItems
                .GroupBy(i => productCats.TryGetValue(i.ProductId, out var cat) ? cat : "N/A")
                .Select(g => new CategoryRevenueDto
                {
                    CategoryName = g.Key,
                    Revenue      = g.Sum(x => x.TotalPrice),
                    SalesCount   = g.Count()
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            _logger.LogInformation(
                "Sales report generated: {From} → {To} | {Count} sales | {Revenue}€",
                from.ToString("yyyy-MM-dd"), to.ToString("yyyy-MM-dd"),
                totalSales, totalRevenue);

            return new SalesReportDto
            {
                From         = from,
                To           = to,
                TotalRevenue = totalRevenue,
                TotalSales   = totalSales,
                AverageSale  = totalSales > 0 ? totalRevenue / totalSales : 0,
                DailySales   = dailySales,
                TopProducts  = topProducts,
                ByCategory   = byCategory
            };
        }, TimeSpan.FromMinutes(15), ct);
    }

    // ============================================================
    // STOCK REPORT
    // ============================================================

    public async Task<StockReportDto> GetStockReportAsync(
        CancellationToken ct = default)
    {
        return await _cache.GetOrCreateAsync("report:stock", async (t) =>
        {
            var threshold = DateTime.UtcNow.AddDays(7);

            var products = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Where(p => !p.IsDeleted)
                .ToListAsync(t);

            var critical = products
                .Where(p => p.CurrentStock <= p.CriticalStock)
                .OrderBy(p => p.CurrentStock)
                .Select(p => new CriticalProductDto
                {
                    Id            = p.Id,
                    Name          = p.Name,
                    CategoryName  = p.Category?.Name ?? "N/A",
                    CurrentStock  = p.CurrentStock,
                    CriticalStock = p.CriticalStock,
                    ExpirationDate = p.ExpirationDate
                })
                .ToList();

            var expiring = products
                .Where(p => p.ExpirationDate.HasValue
                         && p.ExpirationDate.Value <= threshold)
                .OrderBy(p => p.ExpirationDate)
                .Select(p => new ExpiringProductDto
                {
                    Id             = p.Id,
                    Name           = p.Name,
                    CategoryName   = p.Category?.Name ?? "N/A",
                    CurrentStock   = p.CurrentStock,
                    ExpirationDate = p.ExpirationDate!.Value,
                    DaysRemaining  = (int)(p.ExpirationDate!.Value.Date - DateTime.UtcNow.Date).TotalDays
                })
                .ToList();

            var totalValue = products.Sum(p => p.Price * p.CurrentStock);

            return new StockReportDto
            {
                TotalProducts    = products.Count,
                CriticalCount    = critical.Count,
                ExpiringCount    = expiring.Count,
                TotalStockValue  = totalValue,
                CriticalProducts = critical,
                ExpiringProducts = expiring
            };
        }, TimeSpan.FromMinutes(10), ct);
    }
}
