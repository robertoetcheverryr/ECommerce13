using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Users.API.DTOs;

/// <summary>
/// Datos necesarios para registrar un nuevo usuario.
/// </summary>
public class RegisterUserRequest
{
    /// <summary>
    /// Nombre del usuario. Requerido.
    /// </summary>
    /// <example>María</example>
    [JsonRequired]
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Apellido del usuario. Requerido.
    /// </summary>
    /// <example>González</example>
    [JsonRequired]
    [Required(ErrorMessage = "El apellido es obligatorio.")]
    public string Apellido { get; set; } = string.Empty;

    /// <summary>
    /// Email del usuario. Requerido, único, formato válido.
    /// </summary>
    /// <example>maria@email.com</example>
    [JsonRequired]
    [Required(ErrorMessage = "El email es obligatorio.")]
    [ValidEmail]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña en texto plano. Requerida. Solo se acepta en el request; nunca se expone.
    /// </summary>
    /// <example>MiPassword123!</example>
    [JsonRequired]
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [ValidPassword]
    public string Password { get; set; } = string.Empty;
}
