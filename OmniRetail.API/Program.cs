using System.Text;
using System.Text.Json.Serialization;

using OmniRetail.API.Middleware;

using DotNetEnv;

using Serilog;

using FluentValidation;
using FluentValidation.AspNetCore;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using OmniRetail.Application.Interfaces;
using OmniRetail.Application.Validators;

using OmniRetail.Core.Entities;
using OmniRetail.Core.Enums;
using OmniRetail.Core.Interfaces;

using OmniRetail.Infrastructure.Data;
using OmniRetail.Infrastructure.Services;

using System.Threading.RateLimiting;

//
// ========================================
// LOAD ENVIRONMENT VARIABLES
// ========================================
//
Env.Load();

//
// ========================================
// SERILOG CONFIGURATION
// ========================================
//

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        "Logs/omniretail-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        shared: true)
    .CreateLogger();


var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

//
// ========================================
// ENVIRONMENT VARIABLES
// ========================================
//

var connectionString =
    builder.Configuration["CONNECTION_STRING"]
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

var redisConnection =
    builder.Configuration["ACTIVE_REDIS_CONNECTION"];

var jwtSecret =
    builder.Configuration["JWT_SECRET"];

var jwtIssuer =
    builder.Configuration["JWT_ISSUER"];

var jwtAudience =
    builder.Configuration["JWT_AUDIENCE"];

var corsOrigins =
    builder.Configuration["CORS_ALLOWED_ORIGINS"];

//
// ========================================
// VALIDATE REQUIRED VARIABLES
// ========================================
//

if (string.IsNullOrWhiteSpace(connectionString))
    throw new Exception("CONNECTION_STRING is missing.");

if (string.IsNullOrWhiteSpace(jwtSecret))
    throw new Exception("JWT_SECRET is missing.");

//
// ========================================
// CONTROLLERS + JSON
// ========================================
//

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters
            .Add(new JsonStringEnumConverter());
    });

//
// ========================================
// CORS
// ========================================
//

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        if (!string.IsNullOrWhiteSpace(corsOrigins))
        {
            policy
                .WithOrigins(
                    corsOrigins.Split(',',
                        StringSplitOptions.RemoveEmptyEntries))
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
        else
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});
//
// ========================================
// SWAGGER + JWT
// ========================================
//

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "OmniRetail AI API",
        Version = "v1",
        Description = "Plateforme intelligente de gestion commerciale"
    });

    options.AddSecurityDefinition("Bearer",
        new OpenApiSecurityScheme
        {
            Description = "JWT Authorization",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
});

//
// ========================================
// POSTGRESQL
// ========================================
//

builder.Services.AddDbContext<OmniRetailDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null);

            npgsqlOptions.CommandTimeout(60);
        });

#if DEBUG
    options.EnableSensitiveDataLogging();
    options.EnableDetailedErrors();
#endif
});
//
// ========================================
// REDIS
// ========================================
//

if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "OmniRetail_";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

//
// ========================================
// RATE LIMITING
// ========================================
//

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter =
        PartitionedRateLimiter.Create<HttpContext, string>(
            context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User.Identity?.Name
                                  ?? context.Connection.RemoteIpAddress?.ToString()
                                  ?? "anonymous",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder =
                            QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10
                    }));

    options.RejectionStatusCode = 429;
});

//
// ========================================
// HEALTH CHECKS
// ========================================
//

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString);

//
// ========================================
// DEPENDENCY INJECTION
// ========================================
//

// Core
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Products
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

// Inventory
builder.Services.AddScoped<IInventoryService, InventoryService>();

// POS
builder.Services.AddScoped<ISaleService, SaleService>();

// Dashboard
builder.Services.AddScoped<IDashboardService, DashboardService>();

//
// ========================================
// BUG FIX #3 — AddHttpClient DÉPLACÉ AVANT app.Build()
// ----------------------------------------
// PROBLÈME ORIGINAL :
//   builder.Services.AddHttpClient("Anthropic", ...) était appelé
//   APRÈS var app = builder.Build(), à la ligne 315 du fichier original.
//
//   Une fois builder.Build() appelé, le conteneur DI est figé.
//   Tout enregistrement de service après cette ligne est SILENCIEUSEMENT IGNORÉ.
//   Le HttpClient "Anthropic" n'était donc jamais enregistré dans le DI,
//   causant un crash runtime dès que l'AiController tentait de l'injecter.
//
// CORRECTION : déplacé ici, AVANT builder.Build().
// ========================================
//

builder.Services.AddHttpClient("Anthropic", client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});

//
// ========================================
// FLUENT VALIDATION
// ========================================
//

builder.Services.AddValidatorsFromAssemblyContaining<
    CreateCategoryRequestValidator>();

builder.Services.AddFluentValidationAutoValidation();

//
// ========================================
// JWT AUTHENTICATION
// ========================================
//

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSecret)),

                ClockSkew = TimeSpan.Zero
            };
    });

//
// ========================================
// AUTHORIZATION
// ========================================
//

builder.Services.AddAuthorization();

var app = builder.Build();

//
// ========================================
// DATABASE INITIALIZATION
// ========================================
//

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var context =
            services.GetRequiredService<OmniRetailDbContext>();

        await DbSeeder.SeedAsync(context);

        Log.Information("Database initialized.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Database initialization failed.");
    }
}

//
// ========================================
// SWAGGER
// ========================================
//

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = "OmniRetail AI API";
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "OmniRetail API v1");
    });
}

//
// ========================================
// MIDDLEWARES
// ========================================
//

app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseCors("DefaultCorsPolicy");

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

//
// ========================================
// HEALTH CHECK ENDPOINT
// ========================================
//

app.MapHealthChecks("/health");

//
// ========================================
// ROUTES
// ========================================
//

app.MapControllers();

app.MapGet("/", () =>
{
    return Results.Ok(new
    {
        message = "OmniRetail API is running",
        status = "OK",
        timestamp = DateTime.UtcNow
    });
});

Console.WriteLine("🚀 OmniRetail API started.");
Log.Information("OmniRetail API started successfully.");