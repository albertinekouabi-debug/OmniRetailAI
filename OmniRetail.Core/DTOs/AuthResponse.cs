namespace OmniRetail.Core.DTOs;

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public DateTimeOffset AccessTokenExpires { get; set; }

    public DateTimeOffset RefreshTokenExpires { get; set; }

    public Guid SessionId { get; set; }

    public UserDto User { get; set; } = null!;
}