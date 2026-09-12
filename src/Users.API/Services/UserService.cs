namespace Users.API.Services;

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
        var email = Email.Normalize(request.Email);

        /* In real life (LINQ):
        var exists = Users.Any(u =>
            u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        */

        bool exists = false;
        foreach (var candidate in Users)
        {
            if (candidate.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
            {
                exists = true;
                break;
            }
        }

        if (exists)
            throw new BusinessRuleException(
                ErrorCodes.USR_001,
                string.Format(ErrorCodes.USR_001_Message, email),
                ErrorCodes.USR_001_Detail,
                StatusCodes.Status409Conflict);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Nombre = request.Nombre,
            Apellido = request.Apellido,
            Email = email,
            PasswordHash = Password.Hash(request.Password),
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
        var email = Email.Normalize(request.Email);

        /* In real life (LINQ):
        var user = Users.FirstOrDefault(u =>
            u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        */

        User? user = null;
        foreach (var candidate in Users)
        {
            if (candidate.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
            {
                user = candidate;
                break;
            }
        }

        if (user is null || !Password.Matches(request.Password, user.PasswordHash))
            throw new BusinessRuleException(
                ErrorCodes.USR_003,
                ErrorCodes.USR_003_Message,
                ErrorCodes.USR_003_Detail,
                StatusCodes.Status401Unauthorized);

        return user;
    }
}
