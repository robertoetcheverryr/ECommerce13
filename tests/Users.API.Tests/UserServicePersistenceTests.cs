using FluentAssertions;
using Users.API.DTOs;
using Users.API.Exceptions;
using Users.API.Services;

namespace Users.API.Tests;

public class UserServicePersistenceTests
{
    [Fact]
    public void Register_ShouldBeVisibleToANewServiceInstance()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"users-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";

        try
        {
            new DatabaseInitializer(connectionString).Initialize();

            var writer = new UserService(connectionString);
            var created = writer.Register(new RegisterUserRequest
            {
                Nombre = "María",
                Apellido = "González",
                Email = "maria@email.com",
                Password = "MiPassword123!"
            });

            var reader = new UserService(connectionString);
            var loaded = reader.Login(new LoginRequest
            {
                Email = "maria@email.com",
                Password = "MiPassword123!"
            });

            loaded.Id.Should().Be(created.Id);
            loaded.Nombre.Should().Be("María");
            loaded.Apellido.Should().Be("González");
            loaded.Email.Should().Be("maria@email.com");
            loaded.FechaRegistro.Should().Be(created.FechaRegistro);
            loaded.Activo.Should().BeTrue();
            loaded.IntentosFallidos.Should().Be(0);
            loaded.PasswordHash.Should().NotBeNullOrEmpty();
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
                File.Delete(dbPath);
        }
    }

    [Fact]
    public void FailedAttempts_ShouldLockANewServiceInstance()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"users-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";

        try
        {
            new DatabaseInitializer(connectionString).Initialize();

            var writer = new UserService(connectionString);
            writer.Register(new RegisterUserRequest
            {
                Nombre = "María",
                Apellido = "González",
                Email = "maria@email.com",
                Password = "MiPassword123!"
            });

            for (var attempt = 0; attempt < 3; attempt++)
            {
                Action miss = () => writer.Login(new LoginRequest
                {
                    Email = "maria@email.com",
                    Password = "NotThePassword123!"
                });
                miss.Should().Throw<BusinessRuleException>()
                    .Which.ErrorCode.Should().Be(ErrorCodes.USR_003);
            }

            var reader = new UserService(connectionString);
            Action locked = () => reader.Login(new LoginRequest
            {
                Email = "maria@email.com",
                Password = "MiPassword123!"
            });
            locked.Should().Throw<BusinessRuleException>()
                .Which.ErrorCode.Should().Be(ErrorCodes.USR_004);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
                File.Delete(dbPath);
        }
    }

    [Fact]
    public void MarkManuallyBlocked_ShouldBeVisibleToANewServiceInstance()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"users-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";

        try
        {
            new DatabaseInitializer(connectionString).Initialize();

            var writer = new UserService(connectionString);
            writer.Register(new RegisterUserRequest
            {
                Nombre = "María",
                Apellido = "González",
                Email = "maria@email.com",
                Password = "MiPassword123!"
            });
            writer.MarkManuallyBlocked("maria@email.com");

            var reader = new UserService(connectionString);
            Action blocked = () => reader.Login(new LoginRequest
            {
                Email = "maria@email.com",
                Password = "MiPassword123!"
            });
            blocked.Should().Throw<BusinessRuleException>()
                .Which.ErrorCode.Should().Be(ErrorCodes.USR_005);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
                File.Delete(dbPath);
        }
    }
}
