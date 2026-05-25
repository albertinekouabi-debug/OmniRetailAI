using OmniRetail.Core.Enums;

namespace OmniRetail.Core.Entities;

public class User
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public Role Role { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation Properties
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; }
        = new List<InventoryTransaction>();

    public ICollection<Sale> Sales { get; set; }
        = new List<Sale>();

    public ICollection<AuditLog> AuditLogs { get; set; }
        = new List<AuditLog>();

    public ICollection<RefreshToken> RefreshTokens { get; set; }
        = new List<RefreshToken>();

    public ICollection<UserSession> Sessions { get; set; }
        = new List<UserSession>();
}