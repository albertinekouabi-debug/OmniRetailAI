using System.Text.Json;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

using OmniRetail.Application.Interfaces;

namespace OmniRetail.Infrastructure.Services;

/// <summary>
/// Cache distribué enterprise — Redis avec fallback mémoire automatique.
///
/// Fonctionnalités :
/// - Sérialisation JSON type-safe
/// - GetOrCreate pattern (cache-aside)
/// - Suppression par préfixe
/// - Gestion d'erreurs silencieuse (cache ≠ source de vérité)
/// </summary>
public class CacheService : ICacheService
{
    private readonly IDistributedCache        _cache;
    private readonly ILogger<CacheService>    _logger;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // Clés actives en mémoire pour la suppression par préfixe
    private static readonly HashSet<string> _activeKeys = new();
    private static readonly object _lock = new();

    public CacheService(IDistributedCache cache, ILogger<CacheService> logger)
    {
        _cache  = cache;
        _logger = logger;
    }

    // ──────────────────────────────────────────────────────────
    // GET
    // ──────────────────────────────────────────────────────────

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        where T : class
    {
        try
        {
            var bytes = await _cache.GetAsync(key, ct);
            if (bytes is null || bytes.Length == 0) return null;

            return JsonSerializer.Deserialize<T>(bytes, _json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache GET failed for key {Key}", key);
            return null;
        }
    }

    // ──────────────────────────────────────────────────────────
    // SET
    // ──────────────────────────────────────────────────────────

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null,
        CancellationToken ct = default) where T : class
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _json);

            var opts = new DistributedCacheEntryOptions();

            if (expiry.HasValue)
                opts.AbsoluteExpirationRelativeToNow = expiry.Value;
            else
                opts.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);

            await _cache.SetAsync(key, bytes, opts, ct);

            lock (_lock) { _activeKeys.Add(key); }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache SET failed for key {Key}", key);
        }
    }

    // ──────────────────────────────────────────────────────────
    // REMOVE
    // ──────────────────────────────────────────────────────────

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _cache.RemoveAsync(key, ct);
            lock (_lock) { _activeKeys.Remove(key); }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache REMOVE failed for key {Key}", key);
        }
    }

    // ──────────────────────────────────────────────────────────
    // REMOVE BY PREFIX
    // ──────────────────────────────────────────────────────────

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        List<string> keysToRemove;

        lock (_lock)
        {
            keysToRemove = _activeKeys
                .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        foreach (var key in keysToRemove)
            await RemoveAsync(key, ct);
    }

    // ──────────────────────────────────────────────────────────
    // GET OR CREATE (Cache-Aside pattern)
    // ──────────────────────────────────────────────────────────

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiry = null,
        CancellationToken ct = default) where T : class
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached is not null) return cached;

        var value = await factory(ct);
        await SetAsync(key, value, expiry, ct);

        return value;
    }
}

/// <summary>Clés de cache centralisées</summary>
public static class CacheKeys
{
    public const string DashboardKpis = "dashboard:kpis";
    public const string ProductsList  = "products:list";
    public const string AlertsCount   = "alerts:unread-count";

    public static string Product(Guid id) => $"product:{id}";
    public static string UserPerms(Guid userId) => $"user-perms:{userId}";
}
