using System.Diagnostics;
using System.Text;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using OmniRetail.Application.Interfaces;
using OmniRetail.Core.DTOs;
using OmniRetail.Core.Entities;
using OmniRetail.Infrastructure.Data;

namespace OmniRetail.Infrastructure.Services;

/// <summary>
/// Assistant IA OmniRetail — Intégration Anthropic Claude
///
/// Fonctionnement :
/// 1. Récupère les données métier temps réel (stock, ventes, alertes)
/// 2. Construit un contexte riche pour Claude
/// 3. Appelle l'API Anthropic
/// 4. Retourne une réponse analysée et contextualisée
/// 5. Persiste le log IA pour audit et amélioration
/// </summary>
public class AiAssistantService : IAiAssistantService
{
    private readonly OmniRetailDbContext      _context;
    private readonly IConfiguration           _config;
    private readonly IHttpClientFactory        _httpFactory;
    private readonly ILogger<AiAssistantService> _logger;

    private const string AnthropicModel   = "claude-sonnet-4-5";
    private const string AnthropicApiUrl  = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";

    public AiAssistantService(
        OmniRetailDbContext context,
        IConfiguration config,
        IHttpClientFactory httpFactory,
        ILogger<AiAssistantService> logger)
    {
        _context    = context;
        _config     = config;
        _httpFactory = httpFactory;
        _logger      = logger;
    }

    // ============================================================
    // QUERY
    // ============================================================

    public async Task<AiQueryResponse> QueryAsync(
        AiQueryRequest request,
        Guid userId,
        string username,
        string userRole,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        string? output = null;

        try
        {
            // 1. Collecter le contexte métier
            var businessContext = await BuildBusinessContextAsync(userRole, ct);

            // 2. Construire le prompt système
            var systemPrompt = BuildSystemPrompt(username, userRole, businessContext);

            // 3. Appeler l'API Anthropic
            var answer = await CallAnthropicAsync(systemPrompt, request.Question, ct);

            output  = answer;

            sw.Stop();

            // 4. Persister le log IA
            await PersistAiLogAsync(
                userId, "AssistantQuery",
                request.Question, answer, sw.ElapsedMilliseconds,
                true, null, ct);

            return new AiQueryResponse
            {
                Answer      = answer,
                IsSuccess   = true,
                DurationMs  = sw.ElapsedMilliseconds,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "AI query failed for User {UserId}", userId);

            await PersistAiLogAsync(
                userId, "AssistantQuery",
                request.Question, null, sw.ElapsedMilliseconds,
                false, ex.Message, ct);

            return new AiQueryResponse
            {
                Answer    = "Je suis désolé, je ne peux pas répondre à cette question pour le moment.",
                IsSuccess = false,
                Error     = "Service temporairement indisponible.",
                DurationMs = sw.ElapsedMilliseconds,
                GeneratedAt = DateTime.UtcNow
            };
        }
    }

    // ============================================================
    // BUSINESS CONTEXT
    // ============================================================

    private async Task<BusinessContext> BuildBusinessContextAsync(
        string userRole,
        CancellationToken ct)
    {
        var now        = DateTime.UtcNow;
        var today      = now.Date;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // KPIs Ventes
        var todayRevenue = await _context.Sales
            .AsNoTracking()
            .Where(s => s.CreatedAt >= today)
            .SumAsync(s => (decimal?)s.TotalAmount, ct) ?? 0;

        var monthRevenue = await _context.Sales
            .AsNoTracking()
            .Where(s => s.CreatedAt >= monthStart)
            .SumAsync(s => (decimal?)s.TotalAmount, ct) ?? 0;

        var monthSalesCount = await _context.Sales
            .AsNoTracking()
            .CountAsync(s => s.CreatedAt >= monthStart, ct);

        // Stock
        var criticalProducts = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => !p.IsDeleted && p.CurrentStock <= p.CriticalStock)
            .OrderBy(p => p.CurrentStock)
            .Take(10)
            .Select(p => new
            {
                p.Name,
                Category = p.Category != null ? p.Category.Name : "N/A",
                p.CurrentStock,
                p.CriticalStock
            })
            .ToListAsync(ct);

        var expiringProducts = await _context.Products
            .AsNoTracking()
            .Where(p =>
                !p.IsDeleted &&
                p.ExpirationDate.HasValue &&
                p.ExpirationDate.Value.Date <= today.AddDays(7))
            .OrderBy(p => p.ExpirationDate)
            .Take(10)
            .Select(p => new
            {
                p.Name,
                p.ExpirationDate,
                p.CurrentStock
            })
            .ToListAsync(ct);

        // Top produits du mois
        var topProducts = await _context.SaleItems
            .AsNoTracking()
            .Include(si => si.Sale)
            .Where(si => si.Sale.CreatedAt >= monthStart)
            .GroupBy(si => si.ProductName)
            .Select(g => new
            {
                Name        = g.Key,
                Qty         = g.Sum(x => x.Quantity),
                Revenue     = g.Sum(x => x.TotalPrice)
            })
            .OrderByDescending(x => x.Revenue)
            .Take(5)
            .ToListAsync(ct);

        // Alertes
        var alertsCount = await _context.Alerts
            .AsNoTracking()
            .CountAsync(a => !a.IsRead, ct);

        // Total produits
        var totalProducts = await _context.Products
            .AsNoTracking()
            .CountAsync(p => !p.IsDeleted, ct);

        return new BusinessContext
        {
            TodayRevenue     = todayRevenue,
            MonthRevenue     = monthRevenue,
            MonthSalesCount  = monthSalesCount,
            TotalProducts    = totalProducts,
            UnreadAlerts     = alertsCount,
            CriticalProducts = criticalProducts
                .Select(p => $"{p.Name} ({p.Category}) : stock={p.CurrentStock}/{p.CriticalStock}")
                .ToList(),
            ExpiringProducts = expiringProducts
                .Select(p => $"{p.Name} : expire le {p.ExpirationDate:yyyy-MM-dd}, stock={p.CurrentStock}")
                .ToList(),
            TopProducts = topProducts
                .Select(p => $"{p.Name} : {p.Qty} vendus, {p.Revenue:F2}€")
                .ToList(),
            CurrentDate = now.ToString("yyyy-MM-dd HH:mm"),
            UserRole    = userRole
        };
    }

    // ============================================================
    // PROMPT
    // ============================================================

    private static string BuildSystemPrompt(
        string username,
        string userRole,
        BusinessContext ctx)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Tu es l'Assistant IA intelligent de OmniRetail AI Enterprise.");
        sb.AppendLine($"Tu parles avec {username} ({userRole}).");
        sb.AppendLine("Tu analyses les données métier en temps réel et fournis des insights pertinents.");
        sb.AppendLine("Sois précis, professionnel et concis. Réponds en français.");
        sb.AppendLine();
        sb.AppendLine("=== DONNÉES TEMPS RÉEL ===");
        sb.AppendLine($"Date : {ctx.CurrentDate}");
        sb.AppendLine($"CA aujourd'hui : {ctx.TodayRevenue:F2} €");
        sb.AppendLine($"CA ce mois : {ctx.MonthRevenue:F2} €");
        sb.AppendLine($"Ventes ce mois : {ctx.MonthSalesCount}");
        sb.AppendLine($"Total produits actifs : {ctx.TotalProducts}");
        sb.AppendLine($"Alertes non lues : {ctx.UnreadAlerts}");

        if (ctx.CriticalProducts.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("=== PRODUITS EN STOCK CRITIQUE ===");
            ctx.CriticalProducts.ForEach(p => sb.AppendLine($"- {p}"));
        }

        if (ctx.ExpiringProducts.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("=== PRODUITS EXPIRANT BIENTÔT ===");
            ctx.ExpiringProducts.ForEach(p => sb.AppendLine($"- {p}"));
        }

        if (ctx.TopProducts.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("=== TOP 5 PRODUITS CE MOIS ===");
            ctx.TopProducts.ForEach(p => sb.AppendLine($"- {p}"));
        }

        // Restriction pour les employés
        if (userRole.Equals("Employee", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine();
            sb.AppendLine("IMPORTANT : Tu ne dois PAS partager les données financières");
            sb.AppendLine("(CA, marges, profits) avec cet utilisateur qui est un employé.");
            sb.AppendLine("Réponds uniquement sur les stocks, produits et opérations.");
        }

        return sb.ToString();
    }

    // ============================================================
    // ANTHROPIC API CALL
    // ============================================================

    private async Task<string> CallAnthropicAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken ct)
    {
        var apiKey = _config["Ai:AnthropicApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Anthropic API key not configured. Using simulated response.");
            return await SimulateResponseAsync(userMessage, ct);
        }

        var client = _httpFactory.CreateClient("Anthropic");

        var payload = new
        {
            model      = AnthropicModel,
            max_tokens = 1024,
            system     = systemPrompt,
            messages   = new[]
            {
                new { role = "user", content = userMessage }
            }
        };

        var json    = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("x-api-key",         apiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", AnthropicVersion);

        var response = await client.PostAsync(AnthropicApiUrl, content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Anthropic API error {response.StatusCode}: {err}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        var doc          = JsonDocument.Parse(responseJson);

        return doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString()
            ?? "Pas de réponse.";
    }

    // ============================================================
    // SIMULATION (si pas de clé API)
    // ============================================================

    private async Task<string> SimulateResponseAsync(
        string question, CancellationToken ct)
    {
        await Task.Delay(500, ct);

        var q = question.ToLowerInvariant();

        if (q.Contains("stock") && q.Contains("critique"))
            return "📦 D'après les données actuelles, plusieurs produits ont atteint leur seuil de stock critique. Je vous recommande de passer des commandes de réapprovisionnement pour les produits en dessous du seuil défini.";

        if (q.Contains("expi"))
            return "⚠️ Attention : certains produits expirent dans moins de 7 jours. Priorisez leur vente ou leur retrait pour éviter les pertes.";

        if (q.Contains("vente") || q.Contains("chiffre"))
            return "📊 Les ventes de ce mois sont en bonne progression. Consultez le tableau de bord pour le détail des KPIs financiers.";

        if (q.Contains("produit") && (q.Contains("plus") || q.Contains("vendu")))
            return "🏆 Le top 5 des produits les plus vendus ce mois est disponible dans la section Dashboard > Top Produits.";

        return "Je suis votre assistant OmniRetail. Posez-moi des questions sur vos stocks, ventes, alertes ou performances commerciales pour obtenir des insights en temps réel.";
    }

    // ============================================================
    // PERSIST LOG
    // ============================================================

    private async Task PersistAiLogAsync(
        Guid userId, string operation,
        string input, string? output,
        long durationMs, bool success,
        string? error, CancellationToken ct)
    {
        try
        {
            _context.AILogs.Add(new AILog
            {
                Id         = Guid.NewGuid(),
                Operation  = operation,
                Input      = input,
                Output     = output,
                DurationMs = durationMs,
                UserId     = userId,
                IsSuccess  = success,
                ErrorMessage = error,
                CreatedAt  = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist AI log");
        }
    }

    private record BusinessContext
    {
        public decimal       TodayRevenue     { get; init; }
        public decimal       MonthRevenue     { get; init; }
        public int           MonthSalesCount  { get; init; }
        public int           TotalProducts    { get; init; }
        public int           UnreadAlerts     { get; init; }
        public List<string>  CriticalProducts { get; init; } = new();
        public List<string>  ExpiringProducts { get; init; } = new();
        public List<string>  TopProducts      { get; init; } = new();
        public string        CurrentDate      { get; init; } = string.Empty;
        public string        UserRole         { get; init; } = string.Empty;
    }
}
