using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OmniRetail.Core.Entities;

namespace OmniRetail.Infrastructure.Data;

public class OmniRetailDbContext : DbContext
{
    public OmniRetailDbContext(
        DbContextOptions<OmniRetailDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<InventoryTransaction> InventoryTransactions
        => Set<InventoryTransaction>();

    public DbSet<Sale> Sales => Set<Sale>();

    public DbSet<SaleItem> SaleItems => Set<SaleItem>();

    public DbSet<Alert> Alerts => Set<Alert>();

    public DbSet<AILog> AI_Logs => Set<AILog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(x => x.Username)
            .IsUnique();

        modelBuilder.Entity<Product>()
            .Property(x => x.Price)
            .HasPrecision(18, 2);
    }
}