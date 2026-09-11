namespace Users.API.DTOs;

/// <summary>
/// Usuario devuelto por el registro. PasswordHash nunca se incluye.
/// </summary>
public class UserResponse
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
}
