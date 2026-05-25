using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using OmniRetail.Application.Interfaces;
using OmniRetail.Core.DTOs;
using OmniRetail.Core.Enums;

namespace OmniRetail.API.Controllers;

/// <summary>
/// Auth Controller Enterprise
///
/// POST   /api/auth/login              — Authentification JWT + Refresh Token
/// POST   /api/auth/refresh            — Renouvellement du JWT (rotation token)
/// POST   /api/auth/logout             — Déconnexion (révocation token)
/// POST   /api/auth/logout-all         — Déconnexion tous appareils
/// GET    /api/auth/sessions           — Sessions actives
/// DELETE /api/auth/sessions/{id}      — Révoquer une session
/// GET    /api/auth/me                 — Profil connecté
/// GET    /api/auth/permissions        — Permissions de l'utilisateur
/// POST   /api/auth/change-password    — Changer le mot de passe
/// POST   /api/auth/users              — Créer un utilisateur (Admin)
/// GET    /api/auth/users              — Lister les utilisateurs (Admin)
/// PATCH  /api/auth/users/{id}/role    — Changer le rôle (Admin)
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService  _auth;
    private readonly IAuditService _audit;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService auth,
        IAuditService audit,
        ILogger<AuthController> logger)
    {
        _auth   = auth;
        _audit  = audit;
        _logger = logger;
    }

    // ============================================================
    // LOGIN
    // ============================================================

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var ip = GetIp();
        var ua = GetUserAgent();

        var response = await _auth.Login(request, ip, ua, ct);

        if (response is null)
        {
            await _audit.LogLoginAsync(
                Guid.Empty, request.Username ?? "unknown",
                false, ip, ua, "Invalid credentials", ct);

            return Unauthorized(new { message = "Identifiants incorrects." });
        }

        await _audit.LogLoginAsync(
            response.User.Id, response.User.Username,
            true, ip, ua, ct: ct);

        return Ok(response);
    }

    // ============================================================
    // REFRESH TOKEN
    // ============================================================

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var response = await _auth.RefreshToken(
            request.RefreshToken, GetIp(), GetUserAgent(), ct);

        if (response is null)
            return Unauthorized(new { message = "Token invalide ou expiré." });

        return Ok(response);
    }

    // ============================================================
    // LOGOUT
    // ============================================================

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(
        [FromBody] RevokeTokenRequest request,
        CancellationToken ct)
    {
        var userId   = GetUserId();
        var username = GetUsername();

        var ok = await _auth.RevokeToken(
            request.RefreshToken, request.Reason ?? "Logout", GetIp(), ct);

        if (ok)
            await _audit.LogLogoutAsync(userId, username ?? "unknown", GetIp(), ct);

        return NoContent();
    }

    // ============================================================
    // LOGOUT ALL
    // ============================================================

    [HttpPost("logout-all")]
    [Authorize]
    public async Task<IActionResult> LogoutAll(CancellationToken ct)
    {
        var userId   = GetUserId();
        var username = GetUsername();

        await _auth.RevokeAllTokens(userId, "Global logout", ct);

        await _audit.LogAsync(
            "LogoutAll", userId, username,
            "User", userId.ToString(),
            ipAddress: GetIp(), ct: ct);

        return NoContent();
    }

    // ============================================================
    // SESSIONS
    // ============================================================

    [HttpGet("sessions")]
    [Authorize]
    public async Task<IActionResult> GetSessions(CancellationToken ct)
    {
        var sessions = await _auth.GetActiveSessions(
            GetUserId(), GetSessionId(), ct);

        return Ok(sessions);
    }

    [HttpDelete("sessions/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> RevokeSession(Guid id, CancellationToken ct)
    {
        var ok = await _auth.RevokeSession(id, GetUserId(), ct);

        return ok ? NoContent() : NotFound(new { message = "Session introuvable." });
    }

    // ============================================================
    // PROFIL
    // ============================================================

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var user = await _auth.GetUserById(GetUserId(), ct);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpGet("permissions")]
    [Authorize]
    public async Task<IActionResult> GetPermissions(CancellationToken ct)
    {
        var perms = await _auth.GetUserPermissions(GetUserId(), ct);
        return Ok(new { permissions = perms });
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var ok = await _auth.ChangePassword(
                GetUserId(), request.CurrentPassword, request.NewPassword, ct);

            if (!ok)
                return BadRequest(new { message = "Mot de passe actuel incorrect." });

            await _audit.LogAsync(
                "ChangePassword", GetUserId(), GetUsername(),
                "User", GetUserId().ToString(), ipAddress: GetIp(), ct: ct);

            return Ok(new { message = "Mot de passe modifié avec succès." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ============================================================
    // ADMIN — GESTION UTILISATEURS
    // ============================================================

    [HttpPost("users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var user = await _auth.CreateUser(request, ct);

            await _audit.LogAsync(
                "CreateUser", GetUserId(), GetUsername(),
                "User", user.Id.ToString(),
                newValues: new { user.Username, user.Role },
                ipAddress: GetIp(), ct: ct);

            return CreatedAtAction(nameof(Me), null, user);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUsers(CancellationToken ct)
    {
        var users = await _auth.GetUsers(ct);
        return Ok(users);
    }

    [HttpPatch("users/{id:guid}/role")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateRole(
        Guid id, [FromBody] UpdateRoleRequest request,
        CancellationToken ct)
    {
        var ok = await _auth.UpdateUserRole(id, request.Role, ct);

        if (!ok) return NotFound(new { message = "Utilisateur introuvable." });

        await _audit.LogAsync(
            "UpdateUserRole", GetUserId(), GetUsername(),
            "User", id.ToString(),
            newValues: new { Role = request.Role.ToString() },
            ipAddress: GetIp(), ct: ct);

        return NoContent();
    }

    // ── Helpers ─────────────────────────────────────────────────

    private Guid GetUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    private string? GetUsername()
        => User.FindFirst(ClaimTypes.Name)?.Value
        ?? User.FindFirst("unique_name")?.Value;

    private Guid GetSessionId()
    {
        var s = User.FindFirst("sessionId")?.Value;
        return Guid.TryParse(s, out var id) ? id : Guid.Empty;
    }

    private string? GetIp()
    {
        var fwd = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        return !string.IsNullOrWhiteSpace(fwd)
            ? fwd.Split(',').First().Trim()
            : HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private string GetUserAgent()
        => Request.Headers.UserAgent.ToString();
}

/// <summary>Request DTO de mise à jour de rôle</summary>
public record UpdateRoleRequest([System.ComponentModel.DataAnnotations.Required] Role Role);
