using Microsoft.EntityFrameworkCore;
using OmniRetail.Core.Entities;

namespace OmniRetail.Infrastructure.Data;

/// <summary>
/// DbContext Enterprise — OmniRetail AI
///
/// Tables :
/// ─ Auth       : Users / RefreshTokens / UserSessions
/// ─ Catalogue  : Categories / Products
/// ─ Inventaire : InventoryTransactions / Alerts
/// ─ Ventes     : Sales / SaleItems
/// ─ Sécurité   : AuditLogs / Permissions / RolePermissions
/// ─ IA         : AILogs
/// </summary>
public class OmniRetailDbContext : DbContext
{
    public OmniRetailDbContext(DbContextOptions<OmniRetailDbContext> options)
        : base(options) { }

    // ── Auth ──────────────────────────────────────────────────
    public DbSet<User>          Users          => Set<User>();
    public DbSet<RefreshToken>  RefreshTokens  => Set<RefreshToken>();
    public DbSet<UserSession>   UserSessions   => Set<UserSession>();

    // ── Catalogue ─────────────────────────────────────────────
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product>  Products   => Set<Product>();

    // ── Inventaire ────────────────────────────────────────────
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<Alert>                Alerts                => Set<Alert>();

    // ── Ventes ────────────────────────────────────────────────
    public DbSet<Sale>     Sales     => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();

    // ── Sécurité / Audit ──────────────────────────────────────
    public DbSet<AuditLog>       AuditLogs       => Set<AuditLog>();
    public DbSet<Permission>     Permissions     => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    // ── IA ────────────────────────────────────────────────────
    public DbSet<AILog> AILogs => Set<AILog>();

    // ============================================================
    // MODEL CONFIGURATION
    // ============================================================

    protected override void OnModelCreating(ModelBuilder m)
    {
        base.OnModelCreating(m);

        ConfigureUser(m);
        ConfigureRefreshToken(m);
        ConfigureUserSession(m);
        ConfigureCategory(m);
        ConfigureProduct(m);
        ConfigureInventoryTransaction(m);
        ConfigureAlert(m);
        ConfigureSale(m);
        ConfigureSaleItem(m);
        ConfigureAuditLog(m);
        ConfigurePermission(m);
        ConfigureRolePermission(m);
        ConfigureAILog(m);
    }

    // ── User ─────────────────────────────────────────────────
    private static void ConfigureUser(ModelBuilder m)
        => m.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Username).IsRequired().HasMaxLength(100);
            e.Property(x => x.PasswordHash).IsRequired();
            e.Property(x => x.Role).IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.HasIndex(x => x.Username).IsUnique();
        });

    // ── RefreshToken ─────────────────────────────────────────
    private static void ConfigureRefreshToken(ModelBuilder m)
        => m.Entity<RefreshToken>(e =>
        {
            e.ToTable("RefreshTokens");
            e.HasKey(x => x.Id);
            e.Property(x => x.Token).IsRequired().HasMaxLength(200);
            e.Property(x => x.IpAddress).HasMaxLength(50);
            e.Property(x => x.UserAgent).HasMaxLength(500);
            e.Property(x => x.DeviceName).HasMaxLength(100);
            e.Property(x => x.RevokedReason).HasMaxLength(200);
            e.Property(x => x.ReplacedByToken).HasMaxLength(200);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.Token).IsUnique();
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.ExpiresAt);
            e.HasIndex(x => new { x.UserId, x.RevokedAt });
        });

    // ── UserSession ──────────────────────────────────────────
    private static void ConfigureUserSession(ModelBuilder m)
        => m.Entity<UserSession>(e =>
        {
            e.ToTable("UserSessions");
            e.HasKey(x => x.Id);
            e.Property(x => x.IpAddress).HasMaxLength(50);
            e.Property(x => x.UserAgent).HasMaxLength(500);
            e.Property(x => x.DeviceName).HasMaxLength(100);
            e.Property(x => x.Location).HasMaxLength(200);
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.RefreshToken)
             .WithMany()
             .HasForeignKey(x => x.RefreshTokenId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.IsActive);
            e.HasIndex(x => new { x.UserId, x.IsActive });
        });

    // ── Category ─────────────────────────────────────────────
    private static void ConfigureCategory(ModelBuilder m)
        => m.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.HasIndex(x => x.Name).IsUnique();
        });

    // ── Product ──────────────────────────────────────────────
    private static void ConfigureProduct(ModelBuilder m)
        => m.Entity<Product>(e =>
        {
            e.ToTable("Products");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.Price).IsRequired().HasColumnType("decimal(18,2)");
            e.Property(x => x.CurrentStock).IsRequired();
            e.Property(x => x.CriticalStock).IsRequired().HasDefaultValue(5);
            e.Property(x => x.IsDeleted).HasDefaultValue(false);
            e.Property(x => x.IsSensitive).HasDefaultValue(false);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.Property(x => x.ExpirationDate).IsRequired(false);

            e.HasOne(x => x.Category)
             .WithMany(c => c.Products)
             .HasForeignKey(x => x.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.CategoryId);
            e.HasIndex(x => x.IsDeleted);
            e.HasIndex(x => x.CurrentStock);
            e.HasIndex(x => new { x.IsDeleted, x.CurrentStock });

            // Soft delete global query filter
            e.HasQueryFilter(x => !x.IsDeleted);
        });

    // ── InventoryTransaction ─────────────────────────────────
    private static void ConfigureInventoryTransaction(ModelBuilder m)
        => m.Entity<InventoryTransaction>(e =>
        {
            e.ToTable("InventoryTransactions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Quantity).IsRequired();
            e.Property(x => x.Type).IsRequired();
            e.Property(x => x.Reason).HasMaxLength(500);
            e.Property(x => x.Date).HasDefaultValueSql("CURRENT_TIMESTAMP");

            e.HasOne(x => x.Product)
             .WithMany(p => p.InventoryTransactions)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => x.ProductId);
            e.HasIndex(x => x.Date);
            e.HasIndex(x => x.Type);
        });

    // ── Alert ────────────────────────────────────────────────
    // BUG FIX #2 (suite) — HasConversion supprimé car Alert.Severity est maintenant string.
    // Avant : EF Core tentait de stocker un enum (AlertSeverity) en int,
    //         mais HasDefaultValue("Medium") attendait une string → conflit PostgreSQL.
    // Maintenant : Severity est string, HasMaxLength + HasDefaultValue sont cohérents.
    private static void ConfigureAlert(ModelBuilder m)
        => m.Entity<Alert>(e =>
        {
            e.ToTable("Alerts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Message).IsRequired().HasMaxLength(500);
            e.Property(x => x.Severity).HasMaxLength(20).HasDefaultValue("Medium");
            e.Property(x => x.IsRead).HasDefaultValue(false);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.Property(x => x.ReadAt).IsRequired(false);

            e.HasOne(x => x.Product)
             .WithMany()
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.ProductId);
            e.HasIndex(x => x.IsRead);
            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => new { x.ProductId, x.Type, x.IsRead });
        });

    // ── Sale ─────────────────────────────────────────────────
    private static void ConfigureSale(ModelBuilder m)
        => m.Entity<Sale>(e =>
        {
            e.ToTable("Sales");
            e.HasKey(x => x.Id);
            e.Property(x => x.TotalAmount).IsRequired().HasColumnType("decimal(18,2)");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            e.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(x => x.Items)
             .WithOne(si => si.Sale)
             .HasForeignKey(si => si.SaleId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.CreatedAt);
        });

    // ── SaleItem ─────────────────────────────────────────────
    private static void ConfigureSaleItem(ModelBuilder m)
        => m.Entity<SaleItem>(e =>
        {
            e.ToTable("SaleItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.Quantity).IsRequired();
            e.Property(x => x.UnitPrice).IsRequired().HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalPrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.ProductName).IsRequired().HasMaxLength(150);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            e.HasOne(x => x.Sale)
             .WithMany(s => s.Items)
             .HasForeignKey(x => x.SaleId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Product)
             .WithMany(p => p.SaleItems)
             .HasForeignKey(x => x.ProductId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.SaleId);
            e.HasIndex(x => x.ProductId);
        });

    // ── AuditLog ─────────────────────────────────────────────
    private static void ConfigureAuditLog(ModelBuilder m)
        => m.Entity<AuditLog>(e =>
        {
            e.ToTable("AuditLogs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).IsRequired().HasMaxLength(100);
            e.Property(x => x.Username).HasMaxLength(100);
            e.Property(x => x.EntityType).HasMaxLength(100);
            e.Property(x => x.EntityId).HasMaxLength(100);
            e.Property(x => x.IpAddress).HasMaxLength(50);
            e.Property(x => x.UserAgent).HasMaxLength(500);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.Action);
            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => new { x.EntityType, x.EntityId });
        });

    // ── Permission ───────────────────────────────────────────
    private static void ConfigurePermission(ModelBuilder m)
        => m.Entity<Permission>(e =>
        {
            e.ToTable("Permissions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(300);
            e.Property(x => x.Module).IsRequired().HasMaxLength(50);
            e.HasIndex(x => x.Name).IsUnique();
        });

    // ── RolePermission ───────────────────────────────────────
    private static void ConfigureRolePermission(ModelBuilder m)
        => m.Entity<RolePermission>(e =>
        {
            e.ToTable("RolePermissions");
            e.HasKey(x => x.Id);

            e.HasOne(x => x.Permission)
             .WithMany(p => p.RolePermissions)
             .HasForeignKey(x => x.PermissionId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.Role, x.PermissionId }).IsUnique();
            e.HasIndex(x => x.Role);
        });

    // ── AILog ────────────────────────────────────────────────
    private static void ConfigureAILog(ModelBuilder m)
        => m.Entity<AILog>(e =>
        {
            e.ToTable("AILogs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Operation).IsRequired().HasMaxLength(100);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.CreatedAt);
        });
}
