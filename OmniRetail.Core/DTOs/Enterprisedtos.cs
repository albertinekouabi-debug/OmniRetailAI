using System.ComponentModel.DataAnnotations;

namespace OmniRetail.Core.DTOs;

// ================================================================
// AUTH
// ================================================================

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class RevokeTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;

    public string? Reason { get; set; }
}

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}

public class UserSessionDto
{
    public Guid Id { get; set; }

    public string? IpAddress { get; set; }

    public string? DeviceName { get; set; }

    public string? UserAgent { get; set; }

    public string? Location { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset LastActivityAt { get; set; }


    public bool IsCurrent { get; set; }
}


// ================================================================
// AUDIT
// ================================================================

public class AuditLogDto
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string? Username { get; set; }

    public string Action { get; set; } = string.Empty;

    public string? EntityType { get; set; }

    public string? EntityId { get; set; }

    public string? AdditionalInfo { get; set; }

    public string? IpAddress { get; set; }

    public bool IsSuccess { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

// ================================================================
// AI ASSISTANT
// ================================================================

public class AiQueryRequest
{
    [Required]
    [MaxLength(500)]
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// Contexte additionnel (période, filtre, etc.)
    /// </summary>
    public string? Context { get; set; }
}

public class AiQueryResponse
{
    public string Answer { get; set; } = string.Empty;

    public bool IsSuccess { get; set; } = true;

    public string? Error { get; set; }

    public long DurationMs { get; set; }

    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
}

// ================================================================
// PAGINATION
// ================================================================

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();

    public int TotalCount { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalPages =>
        (int)Math.Ceiling((double)TotalCount / PageSize);

    public bool HasNext => Page < TotalPages;

    public bool HasPrevious => Page > 1;
}

// ================================================================
// REPORTS
// ================================================================

public class SalesReportDto
{
    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public decimal TotalRevenue { get; set; }

    public int TotalSales { get; set; }

    public decimal AverageSale { get; set; }

    public List<DailySalesDto> DailySales { get; set; } = new();

    public List<TopProductDto> TopProducts { get; set; } = new();

    public List<CategoryRevenueDto> ByCategory { get; set; } = new();
}

public class DailySalesDto
{
    public DateTime Date { get; set; }

    public decimal Revenue { get; set; }

    public int Count { get; set; }
}

public class CategoryRevenueDto
{
    public string CategoryName { get; set; } = string.Empty;

    public decimal Revenue { get; set; }

    public int SalesCount { get; set; }
}

public class StockReportDto
{
    public int TotalProducts { get; set; }

    public int CriticalCount { get; set; }

    public int ExpiringCount { get; set; }

    public decimal TotalStockValue { get; set; }

    public List<CriticalProductDto> CriticalProducts { get; set; } = new();

    public List<ExpiringProductDto> ExpiringProducts { get; set; } = new();
}

public class ExpiringProductDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public int CurrentStock { get; set; }

    public DateTime ExpirationDate { get; set; }

    public int DaysRemaining { get; set; }
}

// ================================================================
// PERMISSIONS
// ================================================================

public class PermissionDto
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Module { get; set; } = string.Empty;
}