namespace Users.API.DTOs;

/// <summary>
/// Usuario devuelto por el login. PasswordHash nunca se incluye.
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// Identificador único del usuario.
    /// </summary>
    /// <example>a1b2c3d4-0000-0000-0000-111122223333</example>
    public Guid Id { get; set; }

    /// <summary>
    /// Nombre del usuario.
    /// </summary>
    /// <example>María</example>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Apellido del usuario.
    /// </summary>
    /// <example>González</example>
    public string Apellido { get; set; } = string.Empty;

    /// <summary>
    /// Email del usuario.
    /// </summary>
    /// <example>maria@email.com</example>
    public string Email { get; set; } = string.Empty;
}
