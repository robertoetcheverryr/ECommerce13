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
    // Third consecutive miss locks the account. That request stays USR-003.
    private const int MaxConsecutiveFailedLogins = 3;

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

        // Unknown email has no counter. Same USR-003 as a password miss.
        // It is important to NOT differentiate between them because it lets
        // an attacker know the existence of the account.
        if (user is null)
            throw InvalidCredentials();

        // Check the lock before the password so a blocked account does not confirm it.
        // USR-004 is the TP pair Activo == false and IntentosFallidos >= 3.
        // Activo == false with a lower counter is the manual lock (USR-005).
        // Note, should have another field for lock reason to allow extensibility
        // But the blackbox testing means we cannot do it without breaking the SQLite DB.
        if (user.Activo == false && user.IntentosFallidos >= MaxConsecutiveFailedLogins)
            throw Locked(ErrorCodes.USR_004, ErrorCodes.USR_004_Message, ErrorCodes.USR_004_Detail);

        if (user.Activo == false)
            throw Locked(ErrorCodes.USR_005, ErrorCodes.USR_005_Message, ErrorCodes.USR_005_Detail);

        if (!Password.Matches(request.Password, user.PasswordHash))
        {
            user.IntentosFallidos++;
            if (user.IntentosFallidos >= MaxConsecutiveFailedLogins)
                user.Activo = false;

            throw InvalidCredentials();
        }

        user.IntentosFallidos = 0;
        return user;
    }

    /// <summary>
    /// Marca la cuenta como bloqueada sin sumar intentos. No hay endpoint de admin;
    /// sirve para una fila sembrada con Activo en false.
    /// </summary>
    /// <param name="email">Email del usuario ya registrado.</param>
    public void MarkManuallyBlocked(string email)
    {
        var normalized = Email.Normalize(email);

        /* In real life (LINQ):
        var user = Users.FirstOrDefault(u =>
            u.Email.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        */

        User? user = null;
        foreach (var candidate in Users)
        {
            if (candidate.Email.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            {
                user = candidate;
                break;
            }
        }

        if (user is null)
            throw new InvalidOperationException($"No user registered for '{normalized}'.");

        user.Activo = false;
    }

    private static BusinessRuleException Locked(string errorCode, string message, string detail)
        => new(errorCode, message, detail, StatusCodes.Status403Forbidden);

    private static BusinessRuleException InvalidCredentials()
        => new(
            ErrorCodes.USR_003,
            ErrorCodes.USR_003_Message,
            ErrorCodes.USR_003_Detail,
            StatusCodes.Status401Unauthorized);
}
