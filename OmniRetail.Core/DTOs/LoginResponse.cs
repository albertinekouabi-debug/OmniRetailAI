namespace OmniRetail.Core.DTOs;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;

    public DateTime Expiration { get; set; }

    public UserDto User { get; set; } = null!;
}