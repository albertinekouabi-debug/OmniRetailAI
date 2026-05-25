using System.ComponentModel.DataAnnotations;

using OmniRetail.Core.Enums;

namespace OmniRetail.Core.DTOs;

public class CreateUserRequest
{
    [Required(ErrorMessage = "Le nom d'utilisateur est obligatoire.")]
    [MinLength(3, ErrorMessage = "Le nom d'utilisateur doit contenir au moins 3 caractères.")]
    [MaxLength(50, ErrorMessage = "Le nom d'utilisateur ne peut pas dépasser 50 caractères.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
    [MinLength(8, ErrorMessage = "Le mot de passe doit contenir au moins 8 caractères.")]
    public string Password { get; set; } = string.Empty;

    public Role Role { get; set; } = Role.Employee;
}