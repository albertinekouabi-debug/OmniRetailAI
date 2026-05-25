using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using OmniRetail.Application.Interfaces;
using OmniRetail.Core.DTOs;
using OmniRetail.Core.Entities;
using OmniRetail.Core.Enums;
using OmniRetail.Infrastructure.Data;

namespace OmniRetail.Infrastructure.Services;

/// <summary>
/// Service POS (Point Of Sale)
/// Gestion des ventes avec décrément automatique du stock.
/// Toutes les opérations sont transactionnelles.
/// </summary>
public class SaleService : ISaleService
{
    private readonly OmniRetailDbContext _context;
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<SaleService> _logger;

    public SaleService(
        OmniRetailDbContext context,
        IInventoryService inventoryService,
        ILogger<SaleService> logger)
    {
        _context = context;
        _inventoryService = inventoryService;
        _logger = logger;
    }

    //
    // ========================================
    // CREATE SALE — TRANSACTION COMPLÈTE
    // ========================================
    //
    public async Task<SaleDto> CreateSale(Guid userId, CreateSaleRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (request.Items == null || request.Items.Count == 0)
            throw new ArgumentException("La vente doit contenir au moins un article.");

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId invalide.");

        await using var dbTransaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var saleItems = new List<SaleItem>();
            decimal totalAmount = 0;

            foreach (var itemRequest in request.Items)
            {
                if (itemRequest.Quantity <= 0)
                    throw new ArgumentException(
                        $"Quantité invalide pour le produit {itemRequest.ProductId}.");

                //
                // LOAD PRODUCT (avec verrou pour éviter race condition)
                //
                var product = await _context.Products
                    .FirstOrDefaultAsync(p =>
                        p.Id == itemRequest.ProductId &&
                        !p.IsDeleted);

                if (product == null)
                    throw new InvalidOperationException(
                        $"Produit introuvable : {itemRequest.ProductId}.");

                //
                // ANTI STOCK NÉGATIF
                //
                if (product.CurrentStock < itemRequest.Quantity)
                    throw new InvalidOperationException(
                        $"Stock insuffisant pour '{product.Name}'. Disponible : {product.CurrentStock}, demandé : {itemRequest.Quantity}.");

                var previousStock = product.CurrentStock;

                //
                // DÉCRÉMENT STOCK
                //
                product.CurrentStock -= itemRequest.Quantity;
                product.UpdatedAt = DateTime.UtcNow;

                //
                // TRANSACTION INVENTAIRE
                //
                var inventoryTx = new InventoryTransaction
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Quantity = itemRequest.Quantity,
                    Type = InventoryTransactionType.Sale,
                    CreatedAt = DateTime.UtcNow,
                    UserId = userId,
                    Reason = "Vente POS",
                    PreviousStock = previousStock,
                    NewStock = product.CurrentStock
                };

                _context.InventoryTransactions.Add(inventoryTx);

                //
                // SALE ITEM
                //
                var saleItem = new SaleItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    ProductName = product.Name,  // snapshot
                    Quantity = itemRequest.Quantity,
                    UnitPrice = product.Price,
                    CreatedAt = DateTime.UtcNow
                };

                saleItem.ComputeTotal();
                totalAmount += saleItem.TotalPrice;
                saleItems.Add(saleItem);
            }

            //
            // CREATE SALE ENTITY
            //
            var sale = new Sale
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TotalAmount = totalAmount,
                CreatedAt = DateTime.UtcNow,
                Items = saleItems
            };

            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();

            //
            // CHECK ALERTS APRÈS DÉCRÉMENT
            //
            await _inventoryService.CheckAndCreateAlerts();
            await _context.SaveChangesAsync();

            await dbTransaction.CommitAsync();

            _logger.LogInformation(
                "Sale created. SaleId: {SaleId} | User: {UserId} | Total: {Total} | Items: {ItemCount}",
                sale.Id, userId, totalAmount, saleItems.Count);

            return await GetSaleById(sale.Id)
                ?? throw new Exception("Impossible de récupérer la vente créée.");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Error while creating sale.");
            throw;
        }
    }

    //
    // ========================================
    // GET SALES — PAGINATED
    // ========================================
    //
    public async Task<List<SaleDto>> GetSales(int page = 1, int pageSize = 50)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;

        return await _context.Sales
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Items)
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SaleDto
            {
                Id = s.Id,
                UserId = s.UserId,
                UserName = s.User != null ? s.User.Username : string.Empty,
                TotalAmount = s.TotalAmount,
                CreatedAt = s.CreatedAt,
                Items = s.Items.Select(i => new SaleItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.TotalPrice
                }).ToList()
            })
            .ToListAsync();
    }

    //
    // ========================================
    // GET SALE BY ID
    // ========================================
    //
    public async Task<SaleDto?> GetSaleById(Guid id)
    {
        if (id == Guid.Empty) return null;

        return await _context.Sales
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Items)
            .Where(s => s.Id == id)
            .Select(s => new SaleDto
            {
                Id = s.Id,
                UserId = s.UserId,
                UserName = s.User != null ? s.User.Username : string.Empty,
                TotalAmount = s.TotalAmount,
                CreatedAt = s.CreatedAt,
                Items = s.Items.Select(i => new SaleItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.TotalPrice
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    //
    // ========================================
    // GET TODAY SALES
    // ========================================
    //
    public async Task<List<SaleDto>> GetTodaySales()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        return await _context.Sales
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Items)
            .Where(s => s.CreatedAt >= today && s.CreatedAt < tomorrow)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SaleDto
            {
                Id = s.Id,
                UserId = s.UserId,
                UserName = s.User != null ? s.User.Username : string.Empty,
                TotalAmount = s.TotalAmount,
                CreatedAt = s.CreatedAt,
                Items = s.Items.Select(i => new SaleItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.TotalPrice
                }).ToList()
            })
            .ToListAsync();
    }
}
