using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using OmniRetail.Application.Interfaces;
using OmniRetail.Core.DTOs;
using OmniRetail.Core.Entities;
using OmniRetail.Infrastructure.Data;

namespace OmniRetail.Infrastructure.Services;

/// <summary>
/// Service d'audit immuable.
/// Les erreurs d'audit ne bloquent jamais l'opération principale (fire-and-forget sécurisé).
/// </summary>
public class AuditService : IAuditService
{
    private readonly OmniRetailDbContext   _context;
    private readonly ILogger<AuditService> _logger;

    private static readonly JsonSerializerOptions _json = new()
    { WriteIndented = false };

    public AuditService(OmniRetailDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger  = logger;
    }

    // ============================================================
    // WRITE
    // ============================================================

    public async Task LogAsync(
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
        CancellationToken ct = default)
    {
        try
        {
            var log = new AuditLog
            {
                Id             = Guid.NewGuid(),
                Action         = action,
                UserId         = userId,
                Username       = username,
                EntityType     = entityType,
                EntityId       = entityId,
                OldValues      = Serialize(oldValues),
                NewValues      = Serialize(newValues),
                AdditionalInfo = additionalInfo,
                IsSuccess      = isSuccess,
                ErrorMessage   = errorMessage,
                IpAddress      = ipAddress,
                UserAgent      = userAgent,
                CreatedAt      = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // L'audit ne doit jamais crasher l'opération principale
            _logger.LogError(ex, "AuditService: failed to write log for action {Action}", action);
        }
    }

    public Task LogLoginAsync(
        Guid userId, string username,
        bool success, string? ipAddress, string? userAgent,
        string? reason = null,
        CancellationToken ct = default)
        => LogAsync(
            action:        success ? "Login" : "LoginFailed",
            userId:        userId,
            username:      username,
            entityType:    "User",
            entityId:      userId.ToString(),
            additionalInfo: reason,
            isSuccess:     success,
            errorMessage:  success ? null : reason,
            ipAddress:     ipAddress,
            userAgent:     userAgent,
            ct:            ct);

    public Task LogLogoutAsync(
        Guid userId, string username,
        string? ipAddress,
        CancellationToken ct = default)
        => LogAsync(
            action:    "Logout",
            userId:    userId,
            username:  username,
            entityType: "User",
            entityId:  userId.ToString(),
            ipAddress: ipAddress,
            ct:        ct);

    // ============================================================
    // READ
    // ============================================================

    public async Task<PagedResult<AuditLogDto>> GetLogsAsync(
        int page = 1, int pageSize = 50,
        string? action = null,
        Guid? userId = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        if (page < 1)    page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 50;

        var q = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(action))
            q = q.Where(l => l.Action == action);

        if (userId.HasValue)
            q = q.Where(l => l.UserId == userId.Value);

        if (from.HasValue)
            q = q.Where(l => l.CreatedAt >= from.Value);

        if (to.HasValue)
            q = q.Where(l => l.CreatedAt <= to.Value);

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new AuditLogDto
            {
                Id             = l.Id,
                UserId         = l.UserId,
                Username       = l.Username,
                Action         = l.Action,
                EntityType     = l.EntityType,
                EntityId       = l.EntityId,
                AdditionalInfo = l.AdditionalInfo,
                IpAddress      = l.IpAddress,
                IsSuccess      = l.IsSuccess,
                ErrorMessage   = l.ErrorMessage,
                CreatedAt      = l.CreatedAt
            })
            .ToListAsync(ct);

        return new PagedResult<AuditLogDto>
        {
            Items      = items,
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize
        };
    }

    private static string? Serialize(object? value)
        => value is null ? null : JsonSerializer.Serialize(value, _json);
}
