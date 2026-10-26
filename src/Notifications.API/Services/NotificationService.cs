
using System.Globalization;
using Dapper;
using Microsoft.Data.Sqlite;
using Notifications.API.DTOs;
using Notifications.API.Models;

namespace Notifications.API.Services;

/// <summary>
/// Gestiona las notificaciones utilizando SQLite.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly string _connectionString;

    public NotificationService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public Notification SendNotification(SendNotificationRequest request)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UsuarioId = request.UsuarioId,
            Mensaje = request.Mensaje,
            Tipo = request.Tipo,
            Estado = "Enviada",
            FechaEnvio = DateTime.UtcNow
        };

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        connection.Execute("""
            INSERT INTO notifications
                (Id, UsuarioId, Mensaje, Tipo, Estado, FechaEnvio)
            VALUES
                (@Id, @UsuarioId, @Mensaje, @Tipo, @Estado, @FechaEnvio)
            """, new
        {
            Id = notification.Id.ToString(),
            UsuarioId = notification.UsuarioId.ToString(),
            notification.Mensaje,
            notification.Tipo,
            notification.Estado,
            FechaEnvio = notification.FechaEnvio.ToString("o", CultureInfo.InvariantCulture)
        });

        return notification;
    }

    public List<Notification> GetNotificationsByUser(Guid userId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var rows = connection.Query<NotificationRow>("""
            SELECT Id, UsuarioId, Mensaje, Tipo, Estado, FechaEnvio
            FROM notifications
            WHERE UsuarioId = @UsuarioId
            """, new { UsuarioId = userId.ToString() });

        return rows.Select(row => new Notification
        {
            Id = Guid.Parse(row.Id),
            UsuarioId = Guid.Parse(row.UsuarioId),
            Mensaje = row.Mensaje,
            Tipo = row.Tipo,
            Estado = row.Estado,
            FechaEnvio = DateTime.Parse(
                row.FechaEnvio,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind)
        }).ToList();
    }

    private sealed class NotificationRow
    {
        public string Id { get; set; } = string.Empty;
        public string UsuarioId { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string FechaEnvio { get; set; } = string.Empty;
    }
}
