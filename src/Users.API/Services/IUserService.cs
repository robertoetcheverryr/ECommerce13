namespace Users.API.Services;

using Users.API.DTOs;
using Users.API.Models;

/// <summary>
/// Contrato del servicio de usuarios.
/// Contiene la lógica de negocio y acceso a datos (por ahora in-memory).
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Registra un nuevo usuario.
    /// </summary>
    /// <param name="request">Datos del usuario a registrar.</param>
    /// <returns>El usuario creado.</returns>
    /// <exception cref="Exceptions.BusinessRuleException">Cuando el email ya existe (USR-001).</exception>
    User Register(RegisterUserRequest request);

    /// <summary>
    /// Autentica un usuario por email y contraseña.
    /// </summary>
    /// <param name="request">Credenciales.</param>
    /// <returns>El usuario autenticado.</returns>
    /// <exception cref="Exceptions.BusinessRuleException">Cuando las credenciales no coinciden (USR-003).</exception>
    User Login(LoginRequest request);
}
