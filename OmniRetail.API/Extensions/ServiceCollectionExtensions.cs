using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

using FluentValidation;
using FluentValidation.AspNetCore;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using OmniRetail.Application.Interfaces;
using OmniRetail.Application.Validators;
using OmniRetail.Core.Interfaces;
using OmniRetail.Infrastructure.Data;
using OmniRetail.Infrastructure.Services;

namespace OmniRetail.API.Extensions;

/// <summary>
/// Extensions DI — Program.cs lean et lisible.
/// Chaque méthode est autonome, testable, et documentée.
/// </summary>
public static class ServiceCollectionExtensions
{
    // ============================================================
    // CONTROLLERS + JSON
    // ============================================================

    public static IServiceCollection AddApiControllers(
        this IServiceCollection services)
    {
        services
            .AddControllers()
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.Converters
                    .Add(new JsonStringEnumConverter());
                o.JsonSerializerOptions.DefaultIgnoreCondition =
                    JsonIgnoreCondition.WhenWritingNull;
            });

        services.AddEndpointsApiExplorer();

        return services;
    }

    // ============================================================
    // SWAGGER
    // ============================================================

    public static IServiceCollection AddSwaggerEnterprise(
        this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title       = "OmniRetail AI — API Enterprise",
                Version     = "v1",
                Description = "Plateforme SaaS intelligente de gestion commerciale",
                Contact     = new OpenApiContact
                {
                    Name  = "OmniRetail AI Support",
                    Email = "support@omniretail.ai"
                }
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization : **Bearer {token}**",
                Name        = "Authorization",
                In          = ParameterLocation.Header,
                Type        = SecuritySchemeType.Http,
                Scheme      = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {{
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id   = "Bearer"
                    }
                },
                Array.Empty<string>()
            }});
        });

        return services;
    }

    // ============================================================
    // DATABASE
    // ============================================================

    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<OmniRetailDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npg =>
            {
                npg.EnableRetryOnFailure(
                    maxRetryCount:       5,
                    maxRetryDelay:       TimeSpan.FromSeconds(30),
                    errorCodesToAdd:     null);

                npg.CommandTimeout(30);
            });
        });

        return services;
    }

    // ============================================================
    // REDIS CACHE
    // ============================================================

    public static IServiceCollection AddCacheEnterprise(
        this IServiceCollection services,
        string? redisConnection)
    {
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddStackExchangeRedisCache(o =>
            {
                o.Configuration  = redisConnection;
                o.InstanceName   = "OmniRetail_";
            });
        }
        else
        {
            // Fallback mémoire si Redis non disponible
            services.AddDistributedMemoryCache();
        }

        return services;
    }

    // ============================================================
    // JWT AUTHENTICATION
    // ============================================================

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtKey = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is missing.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer              = configuration["Jwt:Issuer"],
                    ValidAudience            = configuration["Jwt:Audience"],
                    IssuerSigningKey         = new SymmetricSecurityKey(
                                                   Encoding.UTF8.GetBytes(jwtKey)),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = ctx =>
                    {
                        var logger = ctx.HttpContext.RequestServices
                            .GetRequiredService<ILogger<JwtBearerEvents>>();
                        logger.LogWarning(
                            "JWT auth failed: {Error}", ctx.Exception.Message);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }

    // ============================================================
    // CORS
    // ============================================================

    public static IServiceCollection AddCorsEnterprise(
        this IServiceCollection services,
        string? allowedOrigins)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("DefaultCorsPolicy", policy =>
            {
                if (!string.IsNullOrWhiteSpace(allowedOrigins))
                {
                    policy
                        .WithOrigins(allowedOrigins
                            .Split(',', StringSplitOptions.RemoveEmptyEntries))
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                }
                else
                {
                    // Développement : autoriser tout
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                }
            });
        });

        return services;
    }

    // ============================================================
    // RATE LIMITING
    // ============================================================

    public static IServiceCollection AddRateLimitingEnterprise(
        this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Global : 100 req/min par IP
            options.GlobalLimiter =
                PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: ctx.User.Identity?.Name
                                      ?? ctx.Connection.RemoteIpAddress?.ToString()
                                      ?? "anonymous",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit          = 100,
                            Window               = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit           = 10
                        }));

            // Login : 10 req/min (anti brute-force)
            options.AddFixedWindowLimiter("login", o =>
            {
                o.PermitLimit          = 10;
                o.Window               = TimeSpan.FromMinutes(1);
                o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                o.QueueLimit           = 0;
            });

            // AI queries : 20 req/min
            options.AddFixedWindowLimiter("ai", o =>
            {
                o.PermitLimit          = 20;
                o.Window               = TimeSpan.FromMinutes(1);
                o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                o.QueueLimit           = 5;
            });

            options.RejectionStatusCode = 429;
        });

        return services;
    }

    // ============================================================
    // HEALTH CHECKS
    // ============================================================

    public static IServiceCollection AddHealthChecksEnterprise(
        this IServiceCollection services,
        string connectionString,
        string? redisConnection)
    {
        var hc = services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "postgresql");

        if (!string.IsNullOrWhiteSpace(redisConnection))
            hc.AddRedis(redisConnection, name: "redis");

        return services;
    }

    // ============================================================
    // APPLICATION SERVICES
    // ============================================================

    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        // Core
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        // Auth enterprise
        services.AddScoped<IAuthService,    AuthService>();
        services.AddScoped<IAuditService,   AuditService>();

        // Cache
        services.AddScoped<ICacheService, CacheService>();

        // Catalogue
        services.AddScoped<IProductService,  ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();

        // Inventaire
        services.AddScoped<IInventoryService, InventoryService>();

        // POS
        services.AddScoped<ISaleService, SaleService>();

        // Dashboard
        services.AddScoped<IDashboardService, DashboardService>();

        // Reports
        services.AddScoped<IReportService, ReportService>();

        // AI
        services.AddScoped<IAiAssistantService, AiAssistantService>();

        // HttpClient pour Anthropic AI
        services.AddHttpClient("Anthropic", c =>
        {
            c.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }

    // ============================================================
    // FLUENT VALIDATION
    // ============================================================

    public static IServiceCollection AddValidationEnterprise(
        this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateCategoryRequestValidator>();
        services.AddFluentValidationAutoValidation();
        return services;
    }
}
