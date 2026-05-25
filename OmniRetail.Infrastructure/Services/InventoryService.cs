using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using OmniRetail.Application.Interfaces;
using OmniRetail.Core.DTOs;
using OmniRetail.Core.Entities;
using OmniRetail.Core.Enums;
using OmniRetail.Infrastructure.Data;

namespace OmniRetail.Infrastructure.Services;

/// <summary>
/// Inventory & Alerts Service
/// Production-ready / SaaS-ready / Audit-ready
/// </summary>
public class InventoryService : IInventoryService
{
    private readonly OmniRetailDbContext _context;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(
        OmniRetailDbContext context,
        ILogger<InventoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    //
    // ========================================
    // GET ALL TRANSACTIONS
    // ========================================
    //
    public async Task<List<InventoryTransactionDto>> GetTransactions()
    {
        return await _context.InventoryTransactions
            .AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.User)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new InventoryTransactionDto
            {
                Id = x.Id,
                ProductId = x.ProductId,
                ProductName = x.Product != null ? x.Product.Name : string.Empty,
                Quantity = x.Quantity,
                Type = x.Type.ToString(),
                Date = x.CreatedAt,
                UserName = x.User != null ? x.User.Username : string.Empty,
                Reason = x.Reason,
                PreviousStock = x.PreviousStock,
                NewStock = x.NewStock
            })
            .ToListAsync();
    }

    //
    // ========================================
    // ADD TRANSACTION
    // ========================================
    //
    public async Task<InventoryTransactionDto> AddTransaction(
        Guid userId,
        CreateInventoryTransactionRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (request.ProductId == Guid.Empty)
            throw new ArgumentException("Invalid product id.");

        if (request.Quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");

        await using var databaseTransaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            //
            // GET PRODUCT — ignore global filter (IgnoreQueryFilters) pour être explicite
            //
            var product = await _context.Products
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x =>
                    x.Id == request.ProductId &&
                    !x.IsDeleted);

            if (product == null)
                throw new InvalidOperationException("Product not found.");

            var previousStock = product.CurrentStock;

            //
            // ANTI STOCK NÉGATIF
            //
            if (request.Type == InventoryTransactionType.Exit &&
                product.CurrentStock < request.Quantity)
            {
                throw new InvalidOperationException(
                    $"Stock insuffisant. Stock actuel : {product.CurrentStock}, demandé : {request.Quantity}.");
            }


            //
            // COMPUTE SIGNED QUANTITY
            //
            int signedQuantity = request.Type switch
            {
                InventoryTransactionType.Entry => request.Quantity,
                InventoryTransactionType.Exit => -request.Quantity,
                InventoryTransactionType.Adjustment => request.Quantity - product.CurrentStock,
                _ => throw new InvalidOperationException("Invalid transaction type.")
            };

            //
            // APPLY STOCK CHANGE
            //
            if (request.Type == InventoryTransactionType.Adjustment)
            {
                product.CurrentStock = request.Quantity;
            }
            else
            {
                product.CurrentStock += signedQuantity;
            }

            //
            // FINAL SAFETY CHECK
            //
            if (product.CurrentStock < 0)
                throw new InvalidOperationException("Negative stock is forbidden.");

            product.UpdatedAt = DateTime.UtcNow;

            //
            // CREATE TRANSACTION ENTITY
            //
            var entity = new InventoryTransaction
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Quantity = request.Type == InventoryTransactionType.Adjustment
                    ? signedQuantity
                    : request.Quantity,
                Type = request.Type,
                CreatedAt = DateTimeOffset.UtcNow,
                UserId = userId == Guid.Empty ? null : userId,
                Reason = string.IsNullOrWhiteSpace(request.Reason)
                    ? "Inventory operation"
                    : request.Reason.Trim(),
                PreviousStock = previousStock,
                NewStock = product.CurrentStock
            };

            _context.InventoryTransactions.Add(entity);
            await _context.SaveChangesAsync();

            //
            // CHECK ALERTS (dans la même transaction)
            //
            await CheckAndCreateAlerts();
            await _context.SaveChangesAsync();

            await databaseTransaction.CommitAsync();

            _logger.LogInformation(
                "Inventory transaction added. Product: {Product} | Type: {Type} | Qty: {Quantity} | Stock: {Prev} → {New}",
                product.Name, entity.Type, entity.Quantity, previousStock, product.CurrentStock);

            return new InventoryTransactionDto
            {
                Id = entity.Id,
                ProductId = entity.ProductId,
                ProductName = product.Name,
                Quantity = entity.Quantity,
                Type = entity.Type.ToString(),
                Date = entity.CreatedAt,
                UserName = string.Empty,
                Reason = entity.Reason,
                PreviousStock = entity.PreviousStock,
                NewStock = entity.NewStock
            };
        }
        catch (Exception ex)
        {
            await databaseTransaction.RollbackAsync();
            _logger.LogError(ex, "Error while adding inventory transaction.");
            throw;
        }
    }

    //
    // ========================================
    // ADJUST STOCK
    // ========================================
    //
    public async Task<InventoryTransactionDto> AdjustStock(
        Guid userId,
        AdjustmentRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        return await AddTransaction(userId, new CreateInventoryTransactionRequest
        {
            ProductId = request.ProductId,
            Quantity = request.NewStock,
            Type = InventoryTransactionType.Adjustment,
            Reason = string.IsNullOrWhiteSpace(request.Reason)
                ? "Manual stock adjustment"
                : request.Reason.Trim()
        });
    }

    //
    // ========================================
    // ALERT ENGINE — CHECK & CREATE ALERTS
    // ========================================
    //
    public async Task CheckAndCreateAlerts()
    {
        var products = await _context.Products
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .ToListAsync();

        foreach (var product in products)
        {
            //
            // LOW STOCK ALERT
            //
            if (product.CurrentStock <= product.CriticalStock)
            {
                var stockAlertExists = await _context.Alerts
                    .AnyAsync(a =>
                        a.ProductId == product.Id &&
                        a.Type == AlertType.StockLow &&
                        !a.IsRead);

                if (!stockAlertExists)
                {
                    _context.Alerts.Add(new Alert
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.Id,
                        Type = AlertType.StockLow,
                        Severity = AlertType.StockLow.GetSeverity(),
                        Message = $"Stock critique pour '{product.Name}'. Stock actuel : {product.CurrentStock} (seuil : {product.CriticalStock})",
                        CreatedAt = DateTimeOffset.UtcNow,
                        IsRead = false
                    });

                    _logger.LogWarning(
                        "Low stock alert created for product {Product}",
                        product.Name);
                }
            }

            //
            // EXPIRATION ALERT
            //
            if (product.ExpirationDate.HasValue)
            {
                var daysBeforeExpiration =
                    (product.ExpirationDate.Value.Date - DateTime.UtcNow.Date).Days;

                if (daysBeforeExpiration <= 7)
                {
                    var expirationAlertExists = await _context.Alerts
                        .AnyAsync(a =>
                            a.ProductId == product.Id &&
                            a.Type == AlertType.Expiring &&
                            !a.IsRead);

                    if (!expirationAlertExists)
                    {
                        _context.Alerts.Add(new Alert
                        {
                            Id = Guid.NewGuid(),
                            ProductId = product.Id,
                            Type = AlertType.Expiring,
                            Severity = AlertType.Expiring.GetSeverity(),
                            Message = daysBeforeExpiration <= 0
                                ? $"Le produit '{product.Name}' est expiré ({product.ExpirationDate:yyyy-MM-dd})."
                                : $"Le produit '{product.Name}' expire dans {daysBeforeExpiration} jour(s) ({product.ExpirationDate:yyyy-MM-dd}).",
                        CreatedAt = DateTimeOffset.UtcNow,
                            IsRead = false
                        });

                        _logger.LogWarning(
                            "Expiration alert created for product {Product}",
                            product.Name);
                    }
                }
            }
        }
    }

    //
    // ========================================
    // GET ALERTS
    // ========================================
    //
    public async Task<List<AlertDto>> GetAlerts(bool unreadOnly = false)
    {
        var query = _context.Alerts
            .AsNoTracking()
            .Include(x => x.Product)
            .AsQueryable();

        if (unreadOnly)
            query = query.Where(x => !x.IsRead);

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AlertDto
            {
                Id = x.Id,
                Type = x.Type.ToString(),
                ProductId = x.ProductId,
                ProductName = x.Product != null ? x.Product.Name : string.Empty,
                Message = x.Message,
                Severity = x.Severity,
                CreatedAt = x.CreatedAt,
                IsRead = x.IsRead,
                ReadAt = x.ReadAt
            })
            .ToListAsync();
    }

    //
    // ========================================
    // GET ALERTS BY PRODUCT
    // ========================================
    //
    public async Task<List<AlertDto>> GetAlertsByProduct(Guid productId)
    {
        if (productId == Guid.Empty)
            return new List<AlertDto>();

        return await _context.Alerts
            .AsNoTracking()
            .Include(x => x.Product)
            .Where(x => x.ProductId == productId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AlertDto
            {
                Id = x.Id,
                Type = x.Type.ToString(),
                ProductId = x.ProductId,
                ProductName = x.Product != null ? x.Product.Name : string.Empty,
                Message = x.Message,
                Severity = x.Severity,
                CreatedAt = x.CreatedAt,
                IsRead = x.IsRead,
                ReadAt = x.ReadAt
            })
            .ToListAsync();
    }

    //
    // ========================================
    // GET UNREAD ALERTS COUNT
    // ========================================
    //
    public async Task<int> GetUnreadAlertsCount()
    {
        return await _context.Alerts
            .AsNoTracking()
            .CountAsync(x => !x.IsRead);
    }

    //
    // ========================================
    // MARK ALERT AS READ
    // ========================================
    //
    public async Task MarkAlertAsRead(Guid alertId)
    {
        if (alertId == Guid.Empty)
            return;

        var alert = await _context.Alerts
            .FirstOrDefaultAsync(x => x.Id == alertId);

        if (alert == null)
        {
            _logger.LogWarning("Alert not found: {AlertId}", alertId);
            return;
        }

        if (alert.IsRead)
            return;

        alert.IsRead = true;
        alert.ReadAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Alert marked as read: {AlertId}", alertId);
    }

    //
    // ========================================
    // MARK ALL ALERTS AS READ
    // ========================================
    //
    public async Task MarkAllAlertsAsRead()
    {
        var unreadAlerts = await _context.Alerts
            .Where(x => !x.IsRead)
            .ToListAsync();

        if (unreadAlerts.Count == 0)
            return;

        var now = DateTimeOffset.UtcNow;
        foreach (var alert in unreadAlerts)
        {
            alert.IsRead = true;
            alert.ReadAt = now;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "{Count} alerts marked as read.",
            unreadAlerts.Count);
    }

    //
    // ========================================
    // CLEANUP OLD ALERTS
    // ========================================
    //
    public async Task CleanupOldAlerts(int days = 30)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-days);

        var oldAlerts = await _context.Alerts
            .Where(x => x.IsRead && x.CreatedAt < cutoff)
            .ToListAsync();

        if (oldAlerts.Count == 0)
            return;

        _context.Alerts.RemoveRange(oldAlerts);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Cleaned up {Count} old alerts (older than {Days} days).",
            oldAlerts.Count, days);
    }
}
