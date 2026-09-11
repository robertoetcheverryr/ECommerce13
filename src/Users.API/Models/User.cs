namespace Users.API.Models;

/// <summary>
/// Usuario del sistema de e-commerce.
/// </summary>
public class User
{
    /// <summary>
    /// Identificador único del usuario.
    /// </summary>
    /// <example>a1b2c3d4-0000-0000-0000-111122223333</example>
    public Guid Id { get; set; }

    /// <summary>
    /// Nombre del usuario. Requerido.
    /// </summary>
    /// <example>María</example>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Apellido del usuario. Requerido.
    /// </summary>
    /// <example>González</example>
    public string Apellido { get; set; } = string.Empty;

    /// <summary>
    /// Email del usuario. Requerido, único, formato válido.
    /// </summary>
    /// <example>maria@email.com</example>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Hash de la contraseña. Nunca se expone en responses.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora de registro en UTC. Se asigna automáticamente al registrar.
    /// </summary>
    /// <example>2024-03-10T09:00:00Z</example>
    public DateTime FechaRegistro { get; set; }

    /// <summary>
    /// false cuando el usuario está bloqueado.
    /// </summary>
    /// <example>true</example>
    public bool Activo { get; set; }

    /// <summary>
    /// Intentos de login fallidos consecutivos. Se resetea al loguearse OK.
    /// </summary>
    /// <example>0</example>
    public int IntentosFallidos { get; set; }
}
