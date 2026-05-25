using FluentValidation;
using OmniRetail.Core.DTOs;

namespace OmniRetail.Application.Validators;

/// <summary>
/// Validation rules for CreateCategoryRequest
/// Production-ready / SaaS-safe
/// </summary>
public class CreateCategoryRequestValidator
	: AbstractValidator<CreateCategoryRequest>
{
	public CreateCategoryRequestValidator()
	{
		//
		// Stop validation after first failure
		//
		RuleLevelCascadeMode = CascadeMode.Stop;

		//
		// CATEGORY NAME
		//
		RuleFor(x => x.Name)

			//
			// Required
			//
			.NotEmpty()
			.WithMessage(
				"Le nom de la catégorie est obligatoire.")

			//
			// Prevent whitespace-only values
			//
			.Must(name =>
				!string.IsNullOrWhiteSpace(name))
			.WithMessage(
				"Le nom ne peut pas être vide.")

			//
			// Normalize validation
			//
			.Must(name =>
				name == null ||
				name.Trim().Length >= 2)
			.WithMessage(
				"Le nom doit contenir au moins 2 caractères.")

			//
			// Length
			//
			.Length(2, 100)
			.WithMessage(
				"Le nom doit contenir entre 2 et 100 caractères.")

			//
			// Safe characters only
			//
			.Matches(@"^[a-zA-ZÀ-ÿ0-9\s\-]+$")
			.WithMessage(
				"Le nom contient des caractères invalides.");
	}
}