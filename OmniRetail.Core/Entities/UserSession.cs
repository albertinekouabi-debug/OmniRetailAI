namespace OmniRetail.Core.Entities;

/// <summary>
/// Session utilisateur — Suivi des connexions actives par appareil.
/// </summary>
public class UserSession : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid RefreshTokenId { get; set; }

    public RefreshToken RefreshToken { get; set; } = null!;

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? DeviceName { get; set; }

    public string? Location { get; set; }

    public DateTimeOffset LastActivityAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? EndedAt { get; set; }

    public bool IsActive { get; set; } = true;
}