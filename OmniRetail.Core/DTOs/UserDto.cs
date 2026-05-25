namespace OmniRetail.Core.DTOs;

public class UserDto
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}