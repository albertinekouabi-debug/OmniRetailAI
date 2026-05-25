using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using OmniRetail.Core.Entities;
using OmniRetail.Core.Enums;

namespace OmniRetail.Infrastructure.Data;

/// <summary>
/// DbSeeder Enterprise
/// Migrations · Permissions RBAC · Utilisateurs · Catalogue démo
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(
        OmniRetailDbContext context,
        ILogger? logger = null)
    {
        logger?.LogInformation("Applying migrations...");
        await context.Database.MigrateAsync();
        logger?.LogInformation("Migrations applied.");

        await SeedPermissionsAsync(context, logger);
        await SeedUsersAsync(context, logger);
        await SeedCatalogAsync(context, logger);

        logger?.LogInformation("Database seeded successfully.");
    }

    // ============================================================
    // PERMISSIONS RBAC
    // ============================================================

    private static async Task SeedPermissionsAsync(
        OmniRetailDbContext context, ILogger? logger)
    {
        if (await context.Permissions.AnyAsync()) return;

        logger?.LogInformation("Seeding RBAC permissions...");

        var defs = new[]
        {
            (Permissions.ProductsRead,    "Voir les produits",          "products"),
            (Permissions.ProductsCreate,  "Créer un produit",           "products"),
            (Permissions.ProductsUpdate,  "Modifier un produit",        "products"),
            (Permissions.ProductsDelete,  "Supprimer un produit",       "products"),
            (Permissions.CategoriesRead,  "Voir les catégories",        "categories"),
            (Permissions.CategoriesCreate,"Créer une catégorie",        "categories"),
            (Permissions.CategoriesDelete,"Supprimer une catégorie",    "categories"),
            (Permissions.InventoryRead,   "Voir l'inventaire",          "inventory"),
            (Permissions.InventoryCreate, "Créer un mouvement",         "inventory"),
            (Permissions.InventoryAdjust, "Ajuster le stock",           "inventory"),
            (Permissions.SalesRead,       "Voir les ventes",            "sales"),
            (Permissions.SalesCreate,     "Créer une vente",            "sales"),
            (Permissions.SalesVoid,       "Annuler une vente",          "sales"),
            (Permissions.AnalyticsRead,   "Voir les analytics",         "analytics"),
            (Permissions.AnalyticsExport, "Exporter les analytics",     "analytics"),
            (Permissions.UsersRead,       "Voir les utilisateurs",      "users"),
            (Permissions.UsersCreate,     "Créer un utilisateur",       "users"),
            (Permissions.UsersUpdate,     "Modifier un utilisateur",    "users"),
            (Permissions.UsersDelete,     "Supprimer un utilisateur",   "users"),
            (Permissions.AlertsRead,      "Voir les alertes",           "alerts"),
            (Permissions.AlertsManage,    "Gérer les alertes",          "alerts"),
            (Permissions.AuditRead,       "Voir les logs d'audit",      "audit"),
            (Permissions.SystemSettings,  "Paramètres système",         "system"),
            (Permissions.AiQuery,         "Utiliser l'assistant IA",    "ai"),
            (Permissions.AiAdvanced,      "Fonctions IA avancées",      "ai"),
        };

        var perms = new Dictionary<string, Permission>();

        foreach (var (name, desc, module) in defs)
        {
            var p = new Permission
            {
                Id          = Guid.NewGuid(),
                Name        = name,
                Description = desc,
                Module      = module,
                CreatedAt   = DateTime.UtcNow
            };
            perms[name] = p;
            context.Permissions.Add(p);
        }

        await context.SaveChangesAsync();

        // Admin → toutes les permissions
        foreach (var pname in Permissions.AdminPermissions)
        {
            if (perms.TryGetValue(pname, out var perm))
                context.RolePermissions.Add(new RolePermission
                {
                    Id           = Guid.NewGuid(),
                    Role         = Role.Admin,
                    PermissionId = perm.Id,
                    CreatedAt    = DateTime.UtcNow
                });
        }

        // Employee → permissions limitées
        foreach (var pname in Permissions.EmployeePermissions)
        {
            if (perms.TryGetValue(pname, out var perm))
                context.RolePermissions.Add(new RolePermission
                {
                    Id           = Guid.NewGuid(),
                    Role         = Role.Employee,
                    PermissionId = perm.Id,
                    CreatedAt    = DateTime.UtcNow
                });
        }

        await context.SaveChangesAsync();

        logger?.LogInformation(
            "Permissions seeded: {Count} permissions | 2 roles configured",
            defs.Length);
    }

    // ============================================================
    // USERS
    // ============================================================

    private static async Task SeedUsersAsync(
        OmniRetailDbContext context, ILogger? logger)
    {
        if (await context.Users.AnyAsync()) return;

        logger?.LogInformation("Seeding users...");

        await context.Users.AddRangeAsync(
            new User
            {
                Id           = Guid.NewGuid(),
                Username     = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                Role         = Role.Admin,
                CreatedAt    = DateTime.UtcNow
            },
            new User
            {
                Id           = Guid.NewGuid(),
                Username     = "employee",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Employee123!"),
                Role         = Role.Employee,
                CreatedAt    = DateTime.UtcNow
            });

        await context.SaveChangesAsync();

        logger?.LogInformation("Users seeded: admin / employee");
    }

    // ============================================================
    // CATALOGUE
    // ============================================================

    private static async Task SeedCatalogAsync(
        OmniRetailDbContext context, ILogger? logger)
    {
        if (await context.Categories.AnyAsync()) return;

        logger?.LogInformation("Seeding catalog...");

        var alimentaire      = MakeCategory("Alimentaire");
        var entretien        = MakeCategory("Entretien");
        var boissons         = MakeCategory("Boissons");
        var produitsLaitiers = MakeCategory("Produits Laitiers");
        var boulangerie      = MakeCategory("Boulangerie");
        var hygiene          = MakeCategory("Hygiène");

        await context.Categories.AddRangeAsync(
            alimentaire, entretien, boissons,
            produitsLaitiers, boulangerie, hygiene);

        await context.SaveChangesAsync();

        await context.Products.AddRangeAsync(
            MakeProduct("Lait Entier 1L",        "Lait entier pasteurisé UHT",       produitsLaitiers.Id, 1.20m,  50, 10,  DateTime.UtcNow.AddDays(15)),
            MakeProduct("Savon Liquide 500ml",   "Savon antibactérien hydratant",     entretien.Id,        2.50m,  30,  5,  null),
            MakeProduct("Coca-Cola 1.5L",        "Boisson gazeuse rafraîchissante",   boissons.Id,         1.80m,   8, 10,  DateTime.UtcNow.AddDays(180)),
            MakeProduct("Riz Basmati 5kg",       "Riz basmati extra long grain",      alimentaire.Id,      8.90m,  20,  5,  null),
            MakeProduct("Pain de Mie Complet",   "Pain complet sans conservateur",    boulangerie.Id,      2.10m,  15,  5,  DateTime.UtcNow.AddDays(7)),
            MakeProduct("Dentifrice Blancheur",  "Dentifrice blancheur 75ml",         hygiene.Id,          3.20m,   3,  5,  null),
            MakeProduct("Huile d'Olive 1L",      "Huile d'olive vierge extra",        alimentaire.Id,      6.50m,  25,  5,  null),
            MakeProduct("Jus d'Orange 1L",       "Jus d'orange pur jus sans sucre",  boissons.Id,         2.30m,  18, 10,  DateTime.UtcNow.AddDays(30)));

        await context.SaveChangesAsync();

        logger?.LogInformation("Catalog seeded: 6 categories + 8 products.");
    }

    // ── Helpers ─────────────────────────────────────────────────

    private static Category MakeCategory(string name) => new()
    {
        Id        = Guid.NewGuid(),
        Name      = name,
        CreatedAt = DateTime.UtcNow
    };

    private static Product MakeProduct(
        string name, string desc, Guid categoryId,
        decimal price, int stock, int criticalStock,
        DateTime? expiration) => new()
    {
        Id             = Guid.NewGuid(),
        Name           = name,
        Description    = desc,
        CategoryId     = categoryId,
        Price          = price,
        CurrentStock   = stock,
        CriticalStock  = criticalStock,
        ExpirationDate = expiration,
        IsSensitive    = false,
        IsDeleted      = false,
        CreatedAt      = DateTime.UtcNow
    };
}
