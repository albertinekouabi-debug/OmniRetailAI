using System.ComponentModel.DataAnnotations;

namespace OmniRetail.Core.DTOs;

/// <summary>
/// Request utilisée pour ajuster manuellement le stock d’un produit
/// (inventaire, correction, audit, casse, régularisation)
/// </summary>
public class AdjustmentRequest
{
	/// <summary>
	/// Identifiant du produit concerné
	/// </summary>
	[Required]
	public Guid ProductId { get; set; }

	/// <summary>
	/// Nouveau niveau de stock après ajustement
	/// Doit être >= 0 (pas de stock négatif possible)
	/// </summary>
	[Range(0, int.MaxValue)]
	public int NewStock { get; set; }

	/// <summary>
	/// Raison métier obligatoire pour audit (fortement recommandé)
	/// Exemple: "Inventaire mensuel", "Correction erreur caisse"
	/// </summary>
	[MaxLength(500)]
	public string? Reason { get; set; }

	/// <summary>
	/// Optionnel : utilisateur ou système source de l'ajustement
	/// utile pour traçabilité avancée
	/// </summary>
	[MaxLength(100)]
	public string? Source { get; set; }

	/// <summary>
	/// Indique si l'ajustement est forcé (override système)
	/// utile pour admin ou corrections critiques
	/// </summary>
	public bool IsForced { get; set; } = false;
}