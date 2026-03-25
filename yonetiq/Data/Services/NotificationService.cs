using Dapper;
using YonetIQ.Data.Infrastructure;
using YonetIQ.Data.Models;

namespace YonetIQ.Data.Services;

/// <summary>
/// Uygulama içi bildirim yönetimi.
/// Görev atama, yeni mesaj, not hatırlatması gibi olaylar için bildirim oluşturur ve okur.
/// </summary>
public class NotificationService(IConfiguration config, AuditService auditService)
    : BaseService(config, auditService)
{
    /// <summary>
    /// Yeni bildirim oluşturulduğunda tetiklenir.
    /// MainLayout instance'ları bu event'e subscribe olup kendi circuit'lerini günceller.
    /// int parametresi hedef UserId'dir.
    /// </summary>
    public static event Func<int, Task>? OnNotificationCreated;

    /// <summary>
    /// Yeni bildirim oluşturur ve aktif circuit'leri bilgilendirir.
    /// </summary>
    public async Task<ServiceResult<int>> CreateAsync(
        int userId, string type, string title, string message, string? relatedUrl = null)
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            const string sql = """
                INSERT INTO Notifications (UserId, Type, Title, Message, RelatedUrl, IsRead, CreatedAt)
                OUTPUT INSERTED.Id
                VALUES (@UserId, @Type, @Title, @Message, @RelatedUrl, 0, SYSUTCDATETIME())
                """;

            var id = await conn.ExecuteScalarAsync<int>(sql,
                new { UserId = userId, Type = type, Title = title,
                      Message = message, RelatedUrl = relatedUrl });

            // Tüm aktif MainLayout circuit'lerini bilgilendir (fire-and-forget güvenli)
            if (OnNotificationCreated is not null)
            {
                _ = Task.Run(() => OnNotificationCreated.Invoke(userId));
            }

            LogAction("Notification", "Create", new { UserId = userId, Type = type });
            return id;
        });
    }

    /// <summary>
    /// Kullanıcının son bildirimlerini döner (okunmuş + okunmamış, tarih sıralı).
    /// </summary>
    public async Task<ServiceResult<List<Notification>>> GetRecentAsync(int userId, int limit = 10)
    {
        return await ExecuteServiceAsync<List<Notification>>(async conn =>
        {
            const string sql = """
                SELECT TOP (@Limit) Id, UserId, Type, Title, Message, RelatedUrl, IsRead, CreatedAt
                FROM Notifications
                WHERE UserId = @UserId
                ORDER BY CreatedAt DESC
                """;
            return (await conn.QueryAsync<Notification>(sql,
                new { UserId = userId, Limit = limit })).ToList();
        });
    }

    /// <summary>
    /// Okunmamış bildirim sayısını döner.
    /// </summary>
    public async Task<ServiceResult<int>> GetUnreadCountAsync(int userId)
    {
        return await ExecuteServiceAsync<int>(async conn =>
            await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Notifications WHERE UserId = @UserId AND IsRead = 0",
                new { UserId = userId }));
    }

    /// <summary>
    /// Belirli bir bildirimi okundu olarak işaretler.
    /// </summary>
    public async Task<ServiceResult<int>> MarkAsReadAsync(int notificationId, int userId)
    {
        return await ExecuteServiceAsync<int>(async conn =>
            await conn.ExecuteAsync(
                "UPDATE Notifications SET IsRead = 1, UpdatedAt = GETUTCDATE() WHERE Id = @Id AND UserId = @UserId",
                new { Id = notificationId, UserId = userId }));
    }

    /// <summary>
    /// Kullanıcının tüm okunmamış bildirimlerini okundu olarak işaretler.
    /// </summary>
    public async Task<ServiceResult> MarkAllReadAsync(int userId)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            await conn.ExecuteAsync(
                "UPDATE Notifications SET IsRead = 1, UpdatedAt = GETUTCDATE() WHERE UserId = @UserId AND IsRead = 0",
                new { UserId = userId });
            LogAction("Notification", "MarkAllRead", new { UserId = userId });
        });
    }
}
