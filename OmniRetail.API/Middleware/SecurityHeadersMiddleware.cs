namespace OmniRetail.API.Middleware;

/// <summary>
/// Middleware de sécurité HTTP — Headers enterprise.
///
/// Protection contre :
/// - Clickjacking (X-Frame-Options)
/// - MIME sniffing (X-Content-Type-Options)
/// - XSS (X-XSS-Protection)
/// - Fingerprinting (suppression Server header)
/// - Information disclosure (X-Powered-By)
///
/// HSTS activé (transport sécurisé obligatoire).
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var h = context.Response.Headers;

        h["X-Frame-Options"]             = "DENY";
        h["X-Content-Type-Options"]      = "nosniff";
        h["X-XSS-Protection"]            = "1; mode=block";
        h["Referrer-Policy"]             = "strict-origin-when-cross-origin";
        h["Permissions-Policy"]          = "camera=(), microphone=(), geolocation=()";
        h["Strict-Transport-Security"]   = "max-age=31536000; includeSubDomains";
        h["Content-Security-Policy"]     = "default-src 'none'; frame-ancestors 'none';";

        // Pas de cache sur les réponses API (hors Swagger)
        if (!context.Request.Path.StartsWithSegments("/swagger"))
        {
            h["Cache-Control"] = "no-store, no-cache, must-revalidate, private";
            h["Pragma"]        = "no-cache";
        }

        // Masquer la stack technique
        h.Remove("Server");
        h.Remove("X-Powered-By");
        h.Remove("X-AspNet-Version");
        h.Remove("X-AspNetMvc-Version");

        await _next(context);
    }
}
