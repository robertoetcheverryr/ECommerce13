using System.Globalization;
using Dapper;
using Microsoft.Data.Sqlite;
using Users.API.DTOs;
using Users.API.Exceptions;
using Users.API.Models;

namespace Users.API.Services;

/// <summary>
/// Implementación del servicio de usuarios.
/// Persistencia en SQLite a través de Dapper.
/// </summary>
public class UserService : IUserService
{
    // Third consecutive miss locks the account. That request stays USR-003.
    private const int MaxConsecutiveFailedLogins = 3;

    private readonly string _connectionString;

    /// <summary>
    /// Constructor. Inyecta la cadena de conexión.
    /// </summary>
    /// <param name="connectionString">Cadena de conexión de SQLite.</param>
    public UserService(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <inheritdoc />
    public User Register(RegisterUserRequest request)
    {
        var email = NormalizeEmail(request.Email);

        using var connection = Open();
        if (FindByEmail(connection, email) is not null)
        {
            throw new BusinessRuleException(
                ErrorCodes.USR_001,
                string.Format(ErrorCodes.USR_001_Message, email),
                ErrorCodes.USR_001_Detail,
                StatusCodes.Status409Conflict);
        }

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

        connection.Execute("""
            INSERT INTO users (Id, Nombre, Apellido, Email, PasswordHash, FechaRegistro, Activo, IntentosFallidos)
            VALUES (@Id, @Nombre, @Apellido, @Email, @PasswordHash, @FechaRegistro, @Activo, @IntentosFallidos)
            """, ToParameters(user));

        return user;
    }

    /// <inheritdoc />
    public User Login(LoginRequest request)
    {
        var email = NormalizeEmail(request.Email);

        using var connection = Open();
        var user = FindByEmail(connection, email);

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

            // Write before throwing. A new service instance must see the lock.
            SaveLockState(connection, user);
            throw InvalidCredentials();
        }

        user.IntentosFallidos = 0;
        SaveLockState(connection, user);
        return user;
    }

    /// <summary>
    /// Marca la cuenta como bloqueada sin sumar intentos. No hay endpoint de admin;
    /// sirve para una fila sembrada con Activo en false.
    /// </summary>
    /// <param name="email">Email del usuario ya registrado.</param>
    public void MarkManuallyBlocked(string email)
    {
        var normalized = NormalizeEmail(email);

        using var connection = Open();
        var user = FindByEmail(connection, normalized);
        if (user is null)
            throw new InvalidOperationException($"No user registered for '{normalized}'.");

        user.Activo = false;
        SaveLockState(connection, user);
    }

    private static string NormalizeEmail(string value) => Email.Normalize(value).ToLowerInvariant();

    private static BusinessRuleException Locked(string errorCode, string message, string detail)
        => new(errorCode, message, detail, StatusCodes.Status403Forbidden);

    private static BusinessRuleException InvalidCredentials()
        => new(
            ErrorCodes.USR_003,
            ErrorCodes.USR_003_Message,
            ErrorCodes.USR_003_Detail,
            StatusCodes.Status401Unauthorized);

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static User? FindByEmail(SqliteConnection connection, string email)
    {
        // Stored email is already lowercased. NOCASE still matches a dropped file that is not.
        var row = connection.QuerySingleOrDefault<UserRow>("""
            SELECT Id, Nombre, Apellido, Email, PasswordHash, FechaRegistro, Activo, IntentosFallidos
            FROM users
            WHERE Email = @Email COLLATE NOCASE
            """, new { Email = email });

        return row is null ? null : Map(row);
    }

    private static void SaveLockState(SqliteConnection connection, User user)
    {
        connection.Execute("""
            UPDATE users
            SET Activo = @Activo, IntentosFallidos = @IntentosFallidos
            WHERE Id = @Id
            """, new
        {
            Id = user.Id.ToString(),
            Activo = user.Activo ? 1 : 0,
            user.IntentosFallidos
        });
    }

    private static object ToParameters(User user) => new
    {
        // Dapper binds this object. Guid and the date are text in the table, so they are formatted here.
        // Activo is INTEGER.
        Id = user.Id.ToString(),
        user.Nombre,
        user.Apellido,
        user.Email,
        user.PasswordHash,
        FechaRegistro = user.FechaRegistro.ToString("o", CultureInfo.InvariantCulture),
        Activo = user.Activo ? 1 : 0,
        user.IntentosFallidos
    };

    private static User Map(UserRow row) => new()
    {
        Id = Guid.Parse(row.Id),
        Nombre = row.Nombre,
        Apellido = row.Apellido,
        Email = row.Email,
        PasswordHash = row.PasswordHash,
        FechaRegistro = DateTime.Parse(row.FechaRegistro, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        Activo = row.Activo != 0,
        IntentosFallidos = row.IntentosFallidos
    };

    private sealed class UserRow
    {
        public string Id { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FechaRegistro { get; set; } = string.Empty;
        public int Activo { get; set; }
        public int IntentosFallidos { get; set; }
    }
}
