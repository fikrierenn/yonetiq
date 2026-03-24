using Dapper;
using Microsoft.Data.SqlClient;
using YonetIQ.Data.Models;
using YonetIQ.Data.Infrastructure;

namespace YonetIQ.Data.Services;

/// <summary>
/// Şirket içi mesajlaşma, bildirimler ve sistem duyurularını yöneten servis sınıfıdır.
/// </summary>
public class MessageService(IConfiguration config, AuditService auditService, NotificationService notifSvc) : BaseService(config, auditService)
{
    /// <summary>
    /// Belirli bir kullanıcı için gelen ve giden mesajları tarih sırasına göre listeler.
    /// </summary>
    public async Task<ServiceResult<List<CommunicationMessage>>> GetUserMessagesAsync(int userId, int limit = 50)
    {
        return await ExecuteServiceAsync<List<CommunicationMessage>>(async conn =>
        {
            try
            {
                return (await conn.QueryAsync<CommunicationMessage>(
                    "sp_Message_ListByUser",
                    new { UserId = userId, Limit = limit },
                    commandType: System.Data.CommandType.StoredProcedure)).ToList();
            }
            catch (SqlException ex) when (IsCompatibleWithFallback(ex))
            {
                var deptId = await conn.ExecuteScalarAsync<int?>(
                    "SELECT DepartmentLookupId FROM Users WHERE Id = @Id", new { Id = userId });

                var sql = @"
                    SELECT TOP (@Limit) *
                    FROM CommunicationMessages
                    WHERE ReceiverUserId = @UserId 
                       OR (ReceiverDepartmentId IS NOT NULL AND ReceiverDepartmentId = @DeptId)
                       OR SenderUserId = @UserId
                    ORDER BY CreatedAt DESC";

                return (await conn.QueryAsync<CommunicationMessage>(sql, new { UserId = userId, Limit = limit, DeptId = deptId })).ToList();
            }
        });
    }

    /// <summary>
    /// Yeni bir mesaj gönderir veya sistem bildirimi oluşturur.
    /// </summary>
    public async Task<ServiceResult<int>> SendMessageAsync(CommunicationMessage msg)
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            try
            {
                return await conn.ExecuteScalarAsync<int>(
                    "sp_Message_Send",
                    new
                    {
                        msg.SenderUserId,
                        msg.ReceiverUserId,
                        msg.ReceiverDepartmentId,
                        msg.Body,
                        Type = (int)msg.Type,
                        msg.AttachmentType,
                        msg.AttachmentId
                    },
                    commandType: System.Data.CommandType.StoredProcedure);
            }
            catch (SqlException ex) when (IsCompatibleWithFallback(ex))
            {
                var sql = @"
                    INSERT INTO CommunicationMessages 
                        (SenderUserId, ReceiverUserId, ReceiverDepartmentId, Body, Type, AttachmentType, AttachmentId, CreatedAt)
                    OUTPUT INSERTED.Id
                    VALUES 
                        (@SenderUserId, @ReceiverUserId, @ReceiverDepartmentId, @Body, @Type, @AttachmentType, @AttachmentId, @CreatedAt)";

                var newId = await conn.ExecuteScalarAsync<int>(sql, new
                {
                    msg.SenderUserId,
                    msg.ReceiverUserId,
                    msg.ReceiverDepartmentId,
                    msg.Body,
                    Type = (int)msg.Type,
                    msg.AttachmentType,
                    msg.AttachmentId,
                    CreatedAt = DateTime.UtcNow
                });

                // Bireysel alıcıya bildirim gönder
                if (msg.ReceiverUserId.HasValue && msg.ReceiverUserId.Value > 0)
                {
                    _ = notifSvc.CreateAsync(
                        userId: msg.ReceiverUserId.Value,
                        type: NotificationTypes.NewMessage,
                        title: "Yeni mesaj",
                        message: msg.Body.Length > 80 ? msg.Body[..80] + "…" : msg.Body,
                        relatedUrl: "/mesajlar");
                }

                return newId;
            }
        });
    }

    /// <summary>
    /// Bir mesajı okundu olarak işaretler.
    /// </summary>
    public async Task<ServiceResult> MarkAsReadAsync(int messageId)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            var sql = "UPDATE CommunicationMessages SET IsRead = 1 WHERE Id = @Id";
            await conn.ExecuteAsync(sql, new { Id = messageId });
        });
    }

    /// <summary>
    /// Kullanıcının okunmamış mesaj/bildirim sayısını döner.
    /// </summary>
    public async Task<ServiceResult<int>> GetUnreadCountAsync(int userId)
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            var deptId = await conn.ExecuteScalarAsync<int?>(
                "SELECT DepartmentLookupId FROM Users WHERE Id = @Id", new { Id = userId });

            var sql = @"
                SELECT COUNT(*)
                FROM CommunicationMessages
                WHERE IsRead = 0 
                  AND (ReceiverUserId = @UserId OR (ReceiverDepartmentId IS NOT NULL AND ReceiverDepartmentId = @DeptId))";

            return await conn.ExecuteScalarAsync<int>(sql, new { UserId = userId, DeptId = deptId });
        });
    }
}
