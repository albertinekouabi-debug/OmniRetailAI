using System.ComponentModel.DataAnnotations;

namespace OmniRetail.Core.DTOs;

public class CreateProductRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public Guid CategoryId { get; set; }

    // ========================================
    // BUG FIX #4 — RANGE LOCALE FR → INVARIANT
    // ----------------------------------------
    // PROBLÈME ORIGINAL :
    //   [Range(typeof(decimal), "0,01", "999999999")]
    //   Utilise une virgule "," comme séparateur décimal (locale française).
    //
    //   Range(typeof(decimal), string, string) parse les bornes avec la
    //   culture courante (CurrentCulture). Sur tout système dont la culture
    //   n'est pas fr-FR / fr-BE / etc. (exemple : serveur Linux en-US,
    //   pipeline CI/CD, Docker par défaut), "0,01" est parsé comme :
    //   • soit 1 (la virgule est ignorée) → la borne minimale devient 1.0
    //   • soit une exception de format au démarrage
    //   Dans les deux cas, le comportement est incorrect et inattendu.
    //
    // CORRECTION : Utilisation du point "." (séparateur invariant).
    //   Range(typeof(decimal), "0.01", "999999999") est culture-safe
    //   et fonctionne identiquement sur tous les environnements.
    // ========================================

    [Required]
    [Range(typeof(decimal), "0.01", "999999999")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int CurrentStock { get; set; }

    [Range(0, int.MaxValue)]
    public int CriticalStock { get; set; } = 5;

    public DateTime? ExpirationDate { get; set; }

    public bool IsSensitive { get; set; }
}
