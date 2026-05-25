using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmniRetail.Core.Entities;

public class SaleItem
{
    //
    // =========================
    // Primary Key
    // =========================
    //

    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    //
    // =========================
    // Relationships
    // =========================
    //

    [Required]
    public Guid SaleId { get; set; }

    public Sale Sale { get; set; } = null!;

    [Required]
    public Guid ProductId { get; set; }

    //
    // Optional for Soft Delete
    //

    public Product? Product { get; set; }

    //
    // =========================
    // Business Data
    // =========================
    //

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Range(typeof(decimal), "0.01", "999999999")]
    public decimal UnitPrice { get; set; }

    //
    // Product Snapshot
    //

    [Required]
    [MaxLength(150)]
    public string ProductName { get; set; } = string.Empty;

    //
    // Persisted Total
    //

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }

    //
    // Audit
    //

    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;

    //
    // Helpers
    //

    public void ComputeTotal()
    {
        TotalPrice = Quantity * UnitPrice;
    }
}