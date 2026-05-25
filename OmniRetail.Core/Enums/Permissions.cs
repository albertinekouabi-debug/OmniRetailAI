namespace OmniRetail.Core.Enums;

/// <summary>
/// Constantes des permissions RBAC — Format : {module}.{action}
/// </summary>
public static class Permissions
{
    // Products
    public const string ProductsRead = "products.read";
    public const string ProductsCreate = "products.create";
    public const string ProductsUpdate = "products.update";
    public const string ProductsDelete = "products.delete";

    // Categories
    public const string CategoriesRead = "categories.read";
    public const string CategoriesCreate = "categories.create";
    public const string CategoriesDelete = "categories.delete";

    // Inventory
    public const string InventoryRead = "inventory.read";
    public const string InventoryCreate = "inventory.create";
    public const string InventoryAdjust = "inventory.adjust";

    // Sales / POS
    public const string SalesRead = "sales.read";
    public const string SalesCreate = "sales.create";
    public const string SalesVoid = "sales.void";

    // Analytics / Dashboard
    public const string AnalyticsRead = "analytics.read";
    public const string AnalyticsExport = "analytics.export";

    // Users
    public const string UsersRead = "users.read";
    public const string UsersCreate = "users.create";
    public const string UsersUpdate = "users.update";
    public const string UsersDelete = "users.delete";

    // Alerts
    public const string AlertsRead = "alerts.read";
    public const string AlertsManage = "alerts.manage";

    // Audit
    public const string AuditRead = "audit.read";

    // System
    public const string SystemSettings = "system.settings";

    // AI
    public const string AiQuery = "ai.query";
    public const string AiAdvanced = "ai.advanced";

    // ── Groupes par rôle ─────────────────────────────────────

    public static IReadOnlyList<string> AdminPermissions => new[]
    {
        ProductsRead, ProductsCreate, ProductsUpdate, ProductsDelete,
        CategoriesRead, CategoriesCreate, CategoriesDelete,
        InventoryRead, InventoryCreate, InventoryAdjust,
        SalesRead, SalesCreate, SalesVoid,
        AnalyticsRead, AnalyticsExport,
        UsersRead, UsersCreate, UsersUpdate, UsersDelete,
        AlertsRead, AlertsManage,
        AuditRead,
        SystemSettings,
        AiQuery, AiAdvanced
    };

    public static IReadOnlyList<string> EmployeePermissions => new[]
    {
        ProductsRead,
        CategoriesRead,
        InventoryRead, InventoryCreate,
        SalesRead, SalesCreate,
        AnalyticsRead,
        AlertsRead,
        AiQuery
    };
}