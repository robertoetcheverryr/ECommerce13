namespace Users.API.Services;

using System.Security.Cryptography;
using System.Text;
using Users.API.DTOs;
using Users.API.Exceptions;
using Users.API.Models;

/// <summary>
/// Implementación del servicio de usuarios.
/// Persistencia temporal in-memory hasta que la cátedra entregue la librería.
/// </summary>
public class UserService : IUserService
{
    // Store in-memory compartido. En el futuro se reemplazará por la librería de persistencia.
    private static readonly List<User> Users = new();

    /// <inheritdoc />
    public User Register(RegisterUserRequest request)
    {
        /* In real life (LINQ):
        var exists = Users.Any(u =>
            u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase));
        */

        bool exists = false;
        foreach (var candidate in Users)
        {
            if (candidate.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase))
            {
                exists = true;
                break;
            }
        }

        if (exists)
            throw new BusinessRuleException(
                ErrorCodes.USR_001,
                string.Format(ErrorCodes.USR_001_Message, request.Email),
                ErrorCodes.USR_001_Detail,
                StatusCodes.Status409Conflict);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Nombre = request.Nombre,
            Apellido = request.Apellido,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            FechaRegistro = DateTime.UtcNow,
            Activo = true,
            IntentosFallidos = 0
        };

        Users.Add(user);
        return user;
    }

    /// <inheritdoc />
    public User Login(LoginRequest request)
    {
        /* In real life (LINQ):
        var user = Users.FirstOrDefault(u =>
            u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase));
        */

        User? user = null;
        foreach (var candidate in Users)
        {
            if (candidate.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase))
            {
                user = candidate;
                break;
            }
        }

        if (user is null || user.PasswordHash != HashPassword(request.Password))
            throw new BusinessRuleException(
                ErrorCodes.USR_003,
                ErrorCodes.USR_003_Message,
                ErrorCodes.USR_003_Detail,
                StatusCodes.Status401Unauthorized);

        return user;
    }

    // Temporary stand-in until we decide the real hasher. Never store the raw password.
    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }
}
