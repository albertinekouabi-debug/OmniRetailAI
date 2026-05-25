using OmniRetail.Core.DTOs;

namespace OmniRetail.Application.Interfaces;

/// <summary>
/// Service central de gestion des stocks et alertes
/// </summary>
public interface IInventoryService
{
    // ========================================
    // TRANSACTIONS
    // ========================================

    Task<List<InventoryTransactionDto>>
        GetTransactions();

    Task<InventoryTransactionDto>
        AddTransaction(
            Guid userId,
            CreateInventoryTransactionRequest request);

    Task<InventoryTransactionDto>
        AdjustStock(
            Guid userId,
            AdjustmentRequest request);

    // ========================================
    // ALERTS
    // ========================================

    Task<List<AlertDto>>
        GetAlerts(
            bool unreadOnly = false);

    Task<List<AlertDto>>
        GetAlertsByProduct(
            Guid productId);

    Task<int>
        GetUnreadAlertsCount();

    Task
        MarkAlertAsRead(
            Guid alertId);

    Task
        MarkAllAlertsAsRead();

    // ========================================
    // AUTOMATION
    // ========================================

    Task
        CheckAndCreateAlerts();

    Task
        CleanupOldAlerts(
            int days = 30);
}