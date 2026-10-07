using System.ComponentModel.DataAnnotations;

namespace Users.API.DTOs;

/// <summary>
/// Credenciales para autenticar un usuario.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Email del usuario.
    /// </summary>
    /// <example>maria@email.com</example>
    [Required(ErrorMessage = "El email es obligatorio.")]
    [ValidEmail]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña en texto plano. Solo se acepta en el request; nunca se expone.
    /// </summary>
    /// <example>MiPassword123!</example>
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [ValidPassword]
    public string Password { get; set; } = string.Empty;
}
