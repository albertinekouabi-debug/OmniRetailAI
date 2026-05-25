using OmniRetail.Core.DTOs;

namespace OmniRetail.Application.Interfaces;

// ================================================================
// IAuditService
// ================================================================

/// <summary>Journal d'audit immuable — toutes les opérations critiques</summary>
public interface IAuditService
{
    Task LogAsync(
        string action,
        Guid? userId = null,
        string? username = null,
        string? entityType = null,
        string? entityId = null,
        object? oldValues = null,
        object? newValues = null,
        string? additionalInfo = null,
        bool isSuccess = true,
        string? errorMessage = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken ct = default);

    Task LogLoginAsync(
        Guid userId, string username,
        bool success, string? ipAddress, string? userAgent,
        string? reason = null,
        CancellationToken ct = default);

    Task LogLogoutAsync(
        Guid userId, string username,
        string? ipAddress,
        CancellationToken ct = default);

    Task<PagedResult<AuditLogDto>> GetLogsAsync(
        int page = 1, int pageSize = 50,
        string? action = null,
        Guid? userId = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default);
}

// ================================================================
// ICacheService
// ================================================================

/// <summary>Cache distribué Redis avec fallback mémoire</summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;

    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null,
        CancellationToken ct = default) where T : class;

    Task RemoveAsync(string key, CancellationToken ct = default);

    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);

    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiry = null,
        CancellationToken ct = default) where T : class;
}

// ================================================================
// IAiAssistantService
// ================================================================

/// <summary>
/// Assistant IA intégrant les données métier OmniRetail
/// Utilise l'API Anthropic Claude pour des réponses contextualisées
/// </summary>
public interface IAiAssistantService
{
    Task<AiQueryResponse> QueryAsync(
        AiQueryRequest request,
        Guid userId,
        string username,
        string userRole,
        CancellationToken ct = default);
}

// ================================================================
// IReportService
// ================================================================

/// <summary>Rapports analytics avancés</summary>
public interface IReportService
{
    Task<SalesReportDto> GetSalesReportAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default);

    Task<StockReportDto> GetStockReportAsync(
        CancellationToken ct = default);
}