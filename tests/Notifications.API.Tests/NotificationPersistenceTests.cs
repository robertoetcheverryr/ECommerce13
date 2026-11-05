using Dapper;
using Microsoft.Data.Sqlite;
using Notifications.API.DTOs;
using Notifications.API.Services;

namespace Notifications.API.Tests;

public class NotificationPersistenceTests
{
    [Fact]
    public void SendNotification_ShouldPersistNotificationInSqlite()
    {
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"notifications-test-{Guid.NewGuid()}.db");

        var connectionString = $"Data Source={databasePath};Pooling=False";

        try
        {
            new DatabaseInitializer(connectionString).Initialize();

            var service = new NotificationService(connectionString);
            var userId = Guid.NewGuid();

            var request = new SendNotificationRequest
            {
                UsuarioId = userId,
                Mensaje = "Notificación de prueba",
                Tipo = "Email"
            };

            var sentNotification = service.SendNotification(request);
            var notifications = service.GetNotificationsByUser(userId);

            var savedNotification = Assert.Single(notifications);

            Assert.Equal(sentNotification.Id, savedNotification.Id);
            Assert.Equal(userId, savedNotification.UsuarioId);
            Assert.Equal("Notificación de prueba", savedNotification.Mensaje);
            Assert.Equal("Email", savedNotification.Tipo);
            Assert.Equal("Enviada", savedNotification.Estado);
            Assert.Equal(DateTimeKind.Utc, sentNotification.FechaEnvio.Kind);
            Assert.Equal(sentNotification.FechaEnvio, savedNotification.FechaEnvio);
        }
        finally
        {
            SqliteConnection.ClearAllPools();

            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }
}