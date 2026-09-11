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
    User Register(RegisterUserRequest request);

    /// <summary>
    /// Autentica un usuario por email y contraseña.
    /// </summary>
    /// <param name="request">Credenciales.</param>
    /// <returns>El usuario si las credenciales coinciden; null si no.</returns>
    User? Login(LoginRequest request);
}
