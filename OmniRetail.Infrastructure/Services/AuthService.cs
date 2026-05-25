using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

using OmniRetail.Application.Interfaces;
using OmniRetail.Core.DTOs;
using OmniRetail.Core.Entities;
using OmniRetail.Core.Enums;
using OmniRetail.Core.Interfaces;
using OmniRetail.Infrastructure.Data;

namespace OmniRetail.Infrastructure.Services;

/// <summary>
/// AuthService Enterprise
///
/// ✅ JWT Access Token (durée courte)
/// ✅ Refresh Token avec rotation (anti-replay)
/// ✅ Sessions multi-appareils
/// ✅ Révocation individuelle / globale
/// ✅ RBAC permissions granulaires
/// ✅ Protection vol de token (détection réutilisation)
/// </summary>
public class AuthService : IAuthService
{
    private readonly OmniRetailDbContext _context;
    private readonly IPasswordHasher     _hasher;
    private readonly IConfiguration      _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        OmniRetailDbContext context,
        IPasswordHasher hasher,
        IConfiguration config,
        ILogger<AuthService> logger)
    {
        _context = context;
        _hasher  = hasher;
        _config  = config;
        _logger  = logger;
    }

    // ============================================================
    // LOGIN
    // ============================================================

    public async Task<AuthResponse?> Login(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var username = request.Username?.Trim();

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            _logger.LogWarning("Login: empty credentials from {Ip}", ipAddress);
            return null;
        }

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Username.ToLower() == username.ToLower(), ct);

        if (user is null || !_hasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning(
                "Login failed for [{Username}] from {Ip}", username, ipAddress);
            return null;
        }

        return await CreateAuthResponse(user, ipAddress, userAgent, ct);
    }

    // ============================================================
    // REFRESH TOKEN — ROTATION
    // ============================================================

    public async Task<AuthResponse?> RefreshToken(
        string refreshToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return null;

        var existing = await _context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == refreshToken, ct);

        if (existing is null) return null;

        // Détection de réutilisation → compromission potentielle
        if (existing.IsRevoked)
        {
            _logger.LogCritical(
                "⚠️ Revoked token reused! User {UserId} from {Ip}. Revoking ALL tokens.",
                existing.UserId, ipAddress);

            await RevokeAllTokens(existing.UserId,
                "Security: revoked token reused", ct);
            return null;
        }

        if (existing.IsExpired)
        {
            _logger.LogWarning(
                "Expired refresh token for User {UserId}", existing.UserId);
            return null;
        }

        // Rotation : invalider l'ancien
        existing.RevokedAt     = DateTime.UtcNow;
        existing.RevokedReason = "Rotation";

        var newRefresh = CreateRefreshToken(existing.UserId, ipAddress, userAgent);
        existing.ReplacedByToken = newRefresh.Token;

        // Mettre à jour la session
        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s =>
                s.RefreshTokenId == existing.Id && s.IsActive, ct);

        if (session is not null)
        {
            session.RefreshTokenId  = newRefresh.Id;
            session.LastActivityAt  = DateTime.UtcNow;
            session.IpAddress       = ipAddress ?? session.IpAddress;
        }

        _context.RefreshTokens.Add(newRefresh);
        await _context.SaveChangesAsync(ct);

        var accessToken = GenerateJwt(existing.User);
        var expMin = _config.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 60);

        _logger.LogInformation(
            "Token refreshed for User {UserId} from {Ip}", existing.UserId, ipAddress);

        return new AuthResponse
        {
            AccessToken         = accessToken,
            RefreshToken        = newRefresh.Token,
            AccessTokenExpires  = DateTime.UtcNow.AddMinutes(expMin),
            RefreshTokenExpires = newRefresh.ExpiresAt.UtcDateTime,
            SessionId           = session?.Id ?? Guid.Empty,
            User = MapUserDto(existing.User)
        };
    }

    // ============================================================
    // REVOKE TOKEN
    // ============================================================

    public async Task<bool> RevokeToken(
        string refreshToken,
        string? reason,
        string? ipAddress,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return false;

        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken, ct);

        if (token is null || !token.IsActive) return false;

        token.RevokedAt     = DateTime.UtcNow;
        token.RevokedReason = reason ?? "Logout";

        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s =>
                s.RefreshTokenId == token.Id && s.IsActive, ct);

        if (session is not null)
        {
            session.IsActive  = false;
            session.EndedAt   = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Token revoked for User {UserId} | Reason: {Reason} | IP: {Ip}",
            token.UserId, reason, ipAddress);

        return true;
    }

    // ============================================================
    // REVOKE ALL
    // ============================================================

    public async Task<bool> RevokeAllTokens(
        Guid userId,
        string? reason,
        CancellationToken ct = default)
    {
        var tokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(ct);

        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId && s.IsActive)
            .ToListAsync(ct);

        var now    = DateTime.UtcNow;
        var motive = reason ?? "Global revoke";

        foreach (var t in tokens)
        {
            t.RevokedAt     = now;
            t.RevokedReason = motive;
        }

        foreach (var s in sessions)
        {
            s.IsActive = false;
            s.EndedAt  = now;
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogWarning(
            "All tokens revoked for {UserId} | Reason: {Reason} | Count: {Count}",
            userId, motive, tokens.Count);

        return true;
    }

    // ============================================================
    // SESSIONS
    // ============================================================

    public async Task<List<UserSessionDto>> GetActiveSessions(
        Guid userId,
        Guid currentSessionId,
        CancellationToken ct = default)
    {
        return await _context.UserSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderByDescending(s => s.LastActivityAt)
            .Select(s => new UserSessionDto
            {
                Id             = s.Id,
                IpAddress      = s.IpAddress,
                DeviceName     = s.DeviceName,
                UserAgent      = s.UserAgent,
                Location       = s.Location,
                CreatedAt      = s.CreatedAt,
                LastActivityAt = s.LastActivityAt,
                IsCurrent      = s.Id == currentSessionId
            })
            .ToListAsync(ct);
    }

    public async Task<bool> RevokeSession(
        Guid sessionId,
        Guid userId,
        CancellationToken ct = default)
    {
        var session = await _context.UserSessions
            .Include(s => s.RefreshToken)
            .FirstOrDefaultAsync(
                s => s.Id == sessionId && s.UserId == userId && s.IsActive, ct);

        if (session is null) return false;

        session.IsActive = false;
        session.EndedAt  = DateTime.UtcNow;

        if (session.RefreshToken?.IsActive == true)
        {
            session.RefreshToken.RevokedAt     = DateTime.UtcNow;
            session.RefreshToken.RevokedReason = "Session revoked by user";
        }

        await _context.SaveChangesAsync(ct);

        return true;
    }

    // ============================================================
    // USERS
    // ============================================================

    public async Task<UserDto> CreateUser(
        CreateUserRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var username = request.Username?.Trim()
            ?? throw new ArgumentException("Username required");

        if (await _context.Users.AnyAsync(x =>
            x.Username.ToLower() == username.ToLower(), ct))
            throw new InvalidOperationException("Username already exists");

        var user = new User
        {
            Id           = Guid.NewGuid(),
            Username     = username,
            PasswordHash = _hasher.HashPassword(request.Password),
            Role         = request.Role,
            CreatedAt    = DateTimeOffset.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "User created: {Username} | Role: {Role}", user.Username, user.Role);

        return MapUserDto(user);
    }

    public async Task<List<UserDto>> GetUsers(CancellationToken ct = default)
        => await _context.Users
            .AsNoTracking()
            .OrderBy(x => x.Username)
            .Select(u => new UserDto
            {
                Id        = u.Id,
                Username  = u.Username,
                Role      = u.Role.ToString(),
                CreatedAt = u.CreatedAt
            })
            .ToListAsync(ct);

    public async Task<UserDto?> GetUserById(Guid id, CancellationToken ct = default)
    {
        var u = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return u is null ? null : MapUserDto(u);
    }

    public async Task<bool> UpdateUserRole(
        Guid userId, Role role, CancellationToken ct = default)
    {
        var u = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (u is null) return false;
        u.Role      = role;
        u.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ChangePassword(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken ct = default)
    {
        var u = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId, ct);
        if (u is null) return false;

        if (!_hasher.VerifyPassword(currentPassword, u.PasswordHash))
        {
            _logger.LogWarning("ChangePassword: wrong current password for {UserId}", userId);
            return false;
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            throw new ArgumentException("New password must be at least 8 characters.");

        u.PasswordHash = _hasher.HashPassword(newPassword);
        u.UpdatedAt    = DateTime.UtcNow;

        // Révoquer toutes les sessions par sécurité
        await RevokeAllTokens(userId, "Password changed", ct);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Password changed for User {UserId}", userId);

        return true;
    }

    // ============================================================
    // PERMISSIONS
    // ============================================================

    public async Task<bool> HasPermission(
        Guid userId,
        string permission,
        CancellationToken ct = default)
    {
        var perms = await GetUserPermissions(userId, ct);
        return perms.Contains(permission);
    }

    public async Task<List<string>> GetUserPermissions(
        Guid userId,
        CancellationToken ct = default)
    {
        var user = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null) return new List<string>();

        return await _context.RolePermissions
            .AsNoTracking()
            .Include(rp => rp.Permission)
            .Where(rp => rp.Role == user.Role)
            .Select(rp => rp.Permission.Name)
            .ToListAsync(ct);
    }

    // ============================================================
    // PRIVATE HELPERS
    // ============================================================

    private async Task<AuthResponse> CreateAuthResponse(
        User user,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct)
    {
        var accessToken = GenerateJwt(user);
        var refresh     = CreateRefreshToken(user.Id, ipAddress, userAgent);
        var expMin      = _config.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 60);

        var session = new UserSession
        {
            Id             = Guid.NewGuid(),
            UserId         = user.Id,
            RefreshTokenId = refresh.Id,
            IpAddress      = ipAddress,
            UserAgent      = userAgent,
            DeviceName     = ParseDevice(userAgent),
            LastActivityAt = DateTime.UtcNow,
            IsActive       = true,
            CreatedAt      = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(refresh);
        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Login OK: {Username} | Session: {SessionId} | IP: {Ip}",
            user.Username, session.Id, ipAddress);

        return new AuthResponse
        {
            AccessToken         = accessToken,
            RefreshToken        = refresh.Token,
            AccessTokenExpires  = DateTime.UtcNow.AddMinutes(expMin),
            RefreshTokenExpires = refresh.ExpiresAt.UtcDateTime,
            SessionId           = session.Id,
            User = MapUserDto(user)
        };
    }

    private string GenerateJwt(User user)
    {
        var key = _config["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key not configured");

        var expMin = _config.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 60);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,        user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti,        Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role,                    user.Role.ToString()),
            new Claim("userId",                           user.Id.ToString()),
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds      = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expires    = DateTime.UtcNow.AddMinutes(expMin);

        var token = new JwtSecurityToken(
            issuer:             _config["Jwt:Issuer"],
            audience:           _config["Jwt:Audience"],
            claims:             claims,
            expires:            expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private RefreshToken CreateRefreshToken(
        Guid userId, string? ip, string? ua)
    {
        var days = _config.GetValue<int>("Jwt:RefreshTokenExpirationDays", 30);

        return new RefreshToken
        {
            Id         = Guid.NewGuid(),
            Token      = GenerateSecureToken(),
            UserId     = userId,
            IpAddress  = ip,
            UserAgent  = ua,
            DeviceName = ParseDevice(ua),
            ExpiresAt  = DateTime.UtcNow.AddDays(days),
            CreatedAt  = DateTimeOffset.UtcNow
        };
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private static string ParseDevice(string? ua)
    {
        if (string.IsNullOrWhiteSpace(ua)) return "Unknown";
        if (ua.Contains("iPhone"))     return "iPhone";
        if (ua.Contains("iPad"))       return "iPad";
        if (ua.Contains("Android"))    return "Android";
        if (ua.Contains("Windows NT")) return "Windows PC";
        if (ua.Contains("Macintosh"))  return "Mac";
        if (ua.Contains("Linux"))      return "Linux";
        return "Unknown Device";
    }

    private static UserDto MapUserDto(User u) => new()
    {
        Id        = u.Id,
        Username  = u.Username,
        Role      = u.Role.ToString(),
        CreatedAt = u.CreatedAt
    };
}
