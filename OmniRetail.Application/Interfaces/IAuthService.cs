using OmniRetail.Core.DTOs;
using OmniRetail.Core.Enums;

namespace OmniRetail.Application.Interfaces;

/// <summary>
/// Service d'authentification Enterprise
/// Refresh Tokens · Sessions multi-appareils · RBAC · Audit
/// </summary>
public interface IAuthService
{
    // ── Login / Logout ────────────────────────────────────────

    Task<AuthResponse?> Login(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct = default);

    Task<AuthResponse?> RefreshToken(
        string refreshToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct = default);

    Task<bool> RevokeToken(
        string refreshToken,
        string? reason,
        string? ipAddress,
        CancellationToken ct = default);

    Task<bool> RevokeAllTokens(
        Guid userId,
        string? reason,
        CancellationToken ct = default);

    // ── Sessions ─────────────────────────────────────────────

    Task<List<UserSessionDto>> GetActiveSessions(
        Guid userId,
        Guid currentSessionId,
        CancellationToken ct = default);

    Task<bool> RevokeSession(
        Guid sessionId,
        Guid userId,
        CancellationToken ct = default);

    // ── Users ─────────────────────────────────────────────────

    Task<UserDto> CreateUser(CreateUserRequest request, CancellationToken ct = default);
    Task<List<UserDto>> GetUsers(CancellationToken ct = default);
    Task<UserDto?> GetUserById(Guid id, CancellationToken ct = default);
    Task<bool> UpdateUserRole(Guid userId, Role role, CancellationToken ct = default);
    Task<bool> ChangePassword(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default);

    // ── Permissions ───────────────────────────────────────────

    Task<bool> HasPermission(Guid userId, string permission, CancellationToken ct = default);
    Task<List<string>> GetUserPermissions(Guid userId, CancellationToken ct = default);
}