namespace OmniRetail.Core.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; set; }

    // Use DateTimeOffset for timezone-aware timestamps across the domain
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }
}