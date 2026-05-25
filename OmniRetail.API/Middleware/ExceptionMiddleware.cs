using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace OmniRetail.API.Middleware;

/// <summary>
/// Middleware de gestion centralisée des erreurs — Enterprise.
///
/// - CorrelationId unique par requête (traçabilité)
/// - Catégorisation HTTP précise par type d'exception
/// - Format JSON cohérent pour tous les clients
/// - Détails techniques masqués en production
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate             _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment             _env;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        IHostEnvironment env)
    {
        _next   = next;
        _logger = logger;
        _env    = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // CorrelationId unique par requête
        var correlationId = Guid.NewGuid().ToString("N")[..12];
        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception | CorrelationId: {CorrelationId} | " +
                "Path: {Path} | Method: {Method}",
                correlationId,
                context.Request.Path,
                context.Request.Method);

            await HandleAsync(context, ex, correlationId);
        }
    }

    private async Task HandleAsync(
        HttpContext context, Exception ex, string correlationId)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = ex switch
        {
            UnauthorizedAccessException =>
                (HttpStatusCode.Unauthorized,          "Accès non autorisé."),

            KeyNotFoundException =>
                (HttpStatusCode.NotFound,              "Ressource introuvable."),

            ArgumentNullException e =>
                (HttpStatusCode.BadRequest,            $"Paramètre manquant : {e.ParamName}."),

            ArgumentException e =>
                (HttpStatusCode.BadRequest,            e.Message),

            InvalidOperationException e =>
                (HttpStatusCode.BadRequest,            e.Message),

            DbUpdateConcurrencyException =>
                (HttpStatusCode.Conflict,              "Conflit de données. Réessayez."),

            DbUpdateException =>
                (HttpStatusCode.BadRequest,            "Erreur de mise à jour de base de données."),

            OperationCanceledException =>
                (HttpStatusCode.ServiceUnavailable,    "Requête annulée."),

            HttpRequestException =>
                (HttpStatusCode.BadGateway,            "Erreur de communication avec un service externe."),

            _ =>
                (HttpStatusCode.InternalServerError,   "Une erreur interne est survenue.")
        };

        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            success       = false,
            statusCode    = (int)statusCode,
            message,
            correlationId,
            // Détails uniquement en développement
            detail        = _env.IsDevelopment() ? ex.ToString() : null
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response, _json));
    }
}
