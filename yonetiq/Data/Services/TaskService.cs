using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using YonetIQ.Data.Models;
using YonetIQ.Data.Infrastructure;

namespace YonetIQ.Data.Services;

/// <summary>
/// Görev yönetimi işlemlerini (listeleme, Kanban, durum güncelleme, kaydetme vb.) yürüten servis sınıfıdır.
/// Sistemdeki iş akışlarının ana motorudur; tüm public metotlar ServiceResult ile tutarlı sonuç döner.
/// </summary>
public class TaskService(IConfiguration config, AuditService auditService, NotificationService notifSvc) : BaseService(config, auditService)
{
    /// <summary>
    /// Veritabanına bağlanarak filtre kriterlerine uygun görev listesini döner.
    /// </summary>
    public async Task<ServiceResult<List<Models.TaskItem>>> ListAsync(
        string? assigneeName = null,
        int? assigneeId = null,
        int? statusLookupId = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        return await ExecuteServiceAsync<List<TaskItem>>(async conn =>
        {
            try
            {
                return (await conn.QueryAsync<TaskItem>(
                    "sp_Task_List",
                    new
                    {
                        AssigneeName = assigneeName,
                        AssigneeId = assigneeId,
                        StatusLookupId = statusLookupId,
                        StartDate = startDate?.Date,
                        EndDate = endDate?.Date
                    },
                    commandType: System.Data.CommandType.StoredProcedure)).ToList();
            }
            catch (SqlException ex) when (IsCompatibleWithFallback(ex))
            {
                var sql = @"
                    SELECT * FROM TaskItems 
                    WHERE (1=1) " +
                    (string.IsNullOrEmpty(assigneeName) ? "" : " AND AssigneeName LIKE @AssigneeName") +
                    (assigneeId == null ? "" : " AND AssigneeId = @AssigneeId") +
                    (statusLookupId == null ? "" : " AND StatusLookupId = @StatusLookupId") +
                    (startDate == null ? "" : " AND AssignedAt >= @StartDate") +
                    (endDate == null ? "" : " AND AssignedAt <= @EndDate") +
                    " ORDER BY Id DESC";

                return (await conn.QueryAsync<Models.TaskItem>(sql, new
                {
                    AssigneeName = $"%{assigneeName}%",
                    AssigneeId = assigneeId,
                    StatusLookupId = statusLookupId,
                    StartDate = startDate?.Date,
                    EndDate = endDate?.Date
                })).ToList();
            }
        });
    }

    /// <summary>
    /// Benzersiz ID numarası verilen bir görevin tüm detaylarını getirir.
    /// </summary>
    public async Task<ServiceResult<Models.TaskItem?>> GetAsync(int id)
    {
        return await ExecuteServiceAsync<Models.TaskItem?>(async conn =>
        {
            try
            {
                return await conn.QueryFirstOrDefaultAsync<TaskItem>(
                    "sp_Task_Get",
                    new { Id = id },
                    commandType: System.Data.CommandType.StoredProcedure);
            }
            catch (SqlException ex) when (IsCompatibleWithFallback(ex))
            {
                return await conn.QueryFirstOrDefaultAsync<TaskItem>(
                    "SELECT * FROM TaskItems WHERE Id = @Id", new { Id = id });
            }
        });
    }

    /// <summary>
    /// Görev kaydı oluşturur veya mevcut kaydı günceller.
    /// </summary>
    public async Task<ServiceResult<int>> SaveAsync(Models.TaskItem t)
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            int resultId;
            try
            {
                resultId = await conn.ExecuteScalarAsync<int>(
                    "sp_Task_Save",
                    new
                    {
                        t.Id,
                        t.Title,
                        t.Description,
                        t.AssigneeName,
                        t.AssigneeId,
                        AssignedAt = t.AssignedAt.Date,
                        DueDate = t.DueDate?.Date,
                        t.PriorityLookupId,
                        t.StatusLookupId
                    },
                    commandType: System.Data.CommandType.StoredProcedure);
            }
            catch (SqlException ex) when (IsCompatibleWithFallback(ex))
            {
                if (t.Id == 0)
                {
                    var sql = @"
                        INSERT INTO TaskItems (Title, Description, AssigneeName, AssigneeId, AssignedAt, DueDate, PriorityLookupId, StatusLookupId, SourceEntityType, SourceEntityId, CreatedAt)
                        OUTPUT INSERTED.Id
                        VALUES (@Title, @Description, @AssigneeName, @AssigneeId, @AssignedAt, @DueDate, @PriorityLookupId, @StatusLookupId, @SourceEntityType, @SourceEntityId, @CreatedAt)";
                    resultId = await conn.ExecuteScalarAsync<int>(sql, new
                    {
                        t.Title, t.Description, t.AssigneeName, t.AssigneeId,
                        AssignedAt = t.AssignedAt.Date, DueDate = t.DueDate?.Date,
                        t.PriorityLookupId, t.StatusLookupId,
                        t.SourceEntityType, t.SourceEntityId,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    var sql = @"
                        UPDATE TaskItems
                        SET Title = @Title, Description = @Description, AssigneeName = @AssigneeName, 
                            AssigneeId = @AssigneeId, AssignedAt = @AssignedAt, DueDate = @DueDate, 
                            PriorityLookupId = @PriorityLookupId, StatusLookupId = @StatusLookupId,
                            SourceEntityType = @SourceEntityType, SourceEntityId = @SourceEntityId
                        WHERE Id = @Id";
                    await conn.ExecuteAsync(sql, new
                    {
                        t.Id, t.Title, t.Description, t.AssigneeName, t.AssigneeId,
                        AssignedAt = t.AssignedAt.Date, DueDate = t.DueDate?.Date,
                        t.PriorityLookupId, t.StatusLookupId,
                        t.SourceEntityType, t.SourceEntityId
                    });
                    resultId = t.Id;
                }
            }

            LogAction("Task", t.Id > 0 ? "Update" : "Add", new { Id = resultId, Title = t.Title });

            // Yeni görev atandıysa atanan kişiye bildirim gönder
            if (t.Id == 0 && t.AssigneeId.HasValue && t.AssigneeId.Value > 0)
            {
                _ = notifSvc.CreateAsync(
                    userId: t.AssigneeId.Value,
                    type: NotificationTypes.TaskAssigned,
                    title: "Yeni görev atandı",
                    message: t.Title.Length > 80 ? t.Title[..80] + "…" : t.Title,
                    relatedUrl: "/gorevler");
            }

            return resultId;
        });
    }

    /// <summary>
    /// Bir görevin durumunu (Lookup ID bazlı) hızlıca günceller.
    /// Görev "Tamamlandı" durumuna geçerse bağlı KeyResult otomatik %100 yapılır.
    /// </summary>
    public async Task<ServiceResult> UpdateStatusAsync(int id, int newStatusLookupId)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            try
            {
                await conn.ExecuteAsync(
                    "sp_Task_UpdateStatus",
                    new { Id = id, StatusLookupId = newStatusLookupId },
                    commandType: System.Data.CommandType.StoredProcedure);
            }
            catch (SqlException ex) when (IsCompatibleWithFallback(ex))
            {
                await conn.ExecuteAsync(
                    "UPDATE TaskItems SET StatusLookupId = @StatusLookupId WHERE Id = @Id",
                    new { Id = id, StatusLookupId = newStatusLookupId });
            }
            LogAction("Task", "UpdateStatus", new { Id = id, NewStatusId = newStatusLookupId });

            // Görev tamamlandıysa bağlı KeyResult'ları güncelle
            var isDone = await conn.ExecuteScalarAsync<bool>(
                @"SELECT CASE WHEN l.Value = @DoneVal THEN 1 ELSE 0 END
                  FROM Lookups l WHERE l.Id = @StatusId",
                new { DoneVal = LookupConstants.ValueTamamlandi, StatusId = newStatusLookupId });

            if (isDone)
                await SyncLinkedKeyResultsAsync(conn, id, completed: true);
        });
    }

    /// <summary>
    /// Bir göreve bağlı tüm KeyResult'ları senkronize eder.
    /// completed=true → KR'ı hedef değerine ulaştır (%100)
    /// </summary>
    private static async Task SyncLinkedKeyResultsAsync(
        System.Data.IDbConnection conn, int taskId, bool completed)
    {
        var linkedKrs = (await conn.QueryAsync<(int Id, int ObjectiveId, decimal StartValue,
            decimal TargetValue, string MetricType)>(
            @"SELECT Id, ObjectiveId, StartValue, TargetValue, MetricType
              FROM KeyResults WHERE LinkedTaskId = @TaskId",
            new { TaskId = taskId })).ToList();

        foreach (var kr in linkedKrs)
        {
            decimal newValue = completed
                ? (kr.MetricType == "Boolean" ? 1 : kr.TargetValue)
                : (kr.MetricType == "Boolean" ? 0 : kr.StartValue);
            decimal newProgress = completed ? 100 : 0;

            await conn.ExecuteAsync(
                "UPDATE KeyResults SET CurrentValue = @V, Progress = @P WHERE Id = @Id",
                new { V = newValue, P = newProgress, Id = kr.Id });

            var avgProg = await conn.ExecuteScalarAsync<decimal>(
                "SELECT AVG(Progress) FROM KeyResults WHERE ObjectiveId = @ObjId",
                new { ObjId = kr.ObjectiveId });
            await conn.ExecuteAsync(
                "UPDATE Objectives SET Progress = @P WHERE Id = @Id",
                new { P = avgProg, Id = kr.ObjectiveId });
        }
    }

    /// <summary>
    /// Bir görevi veritabanından kalıcı olarak siler.
    /// </summary>
    public async Task<ServiceResult> DeleteAsync(int id)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            try
            {
                await conn.ExecuteAsync(
                    "sp_Task_Delete",
                    new { Id = id },
                    commandType: System.Data.CommandType.StoredProcedure);
            }
            catch (SqlException ex) when (IsCompatibleWithFallback(ex))
            {
                await conn.ExecuteAsync("DELETE FROM TaskItems WHERE Id = @Id", new { Id = id });
            }
            LogAction("Task", "Delete", new { Id = id });
        });
    }

    /// <summary>
    /// Sistemde henüz tamamlanmamış toplam görev adedini döner.
    /// </summary>
    public async Task<ServiceResult<int>> GetPendingTaskCountAsync()
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            try
            {
                return await conn.ExecuteScalarAsync<int>(
                    "sp_Task_PendingCount",
                    commandType: System.Data.CommandType.StoredProcedure);
            }
            catch (SqlException ex) when (IsCompatibleWithFallback(ex))
            {
                return await conn.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*) FROM TaskItems
                    WHERE StatusLookupId <> (
                        SELECT Id FROM Lookups WHERE [Group] = '" + LookupConstants.GroupTaskStatus + @"' AND Value = '" + LookupConstants.ValueTamamlandi + @"'
                    )");
            }
        });
    }

    /// <summary>
    /// Belirtilen sayı kadar bekleyen (tamamlanmamış) görevi tarih sırasına göre getirir.
    /// </summary>
    public async Task<ServiceResult<List<Models.TaskItem>>> ListPendingTasksAsync(int count = 10)
    {
        return await ExecuteServiceAsync<List<TaskItem>>(async conn =>
        {
            try
            {
                return (await conn.QueryAsync<TaskItem>(
                    "sp_Task_ListPending",
                    new { Count = count },
                    commandType: System.Data.CommandType.StoredProcedure)).ToList();
            }
            catch (SqlException ex) when (IsCompatibleWithFallback(ex))
            {
                return (await conn.QueryAsync<Models.TaskItem>($@"
                    SELECT TOP {count} * FROM TaskItems
                    WHERE StatusLookupId <> (
                        SELECT Id FROM Lookups WHERE [Group] = '{LookupConstants.GroupTaskStatus}' AND Value = '{LookupConstants.ValueTamamlandi}'
                    )
                    ORDER BY CreatedAt DESC")).ToList();
            }
        });
    }

    /// <summary>
    /// Kanban kartlarını oluşturmak için verileri durum bazlı gruplayarak döner.
    /// ListAsync başarısız olursa boş sözlük yerine hata bilgisi ServiceResult ile iletilir.
    /// </summary>
    public async Task<ServiceResult<Dictionary<int, List<Models.TaskItem>>>> GetKanbanDataAsync()
    {
        var result = await ListAsync();
        if (!result.IsSuccess)
            return ServiceResult<Dictionary<int, List<Models.TaskItem>>>.Failure(result.Message, result.ErrorCode);
        if (result.Data == null || result.Data.Count == 0)
            return ServiceResult<Dictionary<int, List<Models.TaskItem>>>.Success(new Dictionary<int, List<Models.TaskItem>>());
        var grouped = result.Data.GroupBy(g => g.StatusLookupId).ToDictionary(g => g.Key, g => g.ToList());
        return ServiceResult<Dictionary<int, List<Models.TaskItem>>>.Success(grouped);
    }

    /// <summary>
    /// Sistemde daha önce manuel girilmiş olan sorumlu isimlerini liste halinde döner.
    /// </summary>
    public async Task<ServiceResult<List<string>>> GetAssigneesAsync()
    {
        return await ExecuteServiceAsync<List<string>>(async conn =>
        {
            try
            {
                return (await conn.QueryAsync<string>(
                    "sp_Task_GetAssignees",
                    commandType: System.Data.CommandType.StoredProcedure)).ToList();
            }
            catch (SqlException ex) when (IsCompatibleWithFallback(ex))
            {
                return (await conn.QueryAsync<string>(
                    "SELECT DISTINCT AssigneeName FROM TaskItems WHERE AssigneeName IS NOT NULL AND AssigneeName <> ''")).ToList();
            }
        });
    }

    /// <summary>Geciken görev sayısını döner (DueDate geçmiş, tamamlanmamış ve iptal edilmemiş)</summary>
    public async Task<ServiceResult<int>> GetOverdueTaskCountAsync()
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            return await conn.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*) FROM TaskItems
                WHERE DueDate < GETUTCDATE()
                AND StatusLookupId NOT IN (
                    SELECT Id FROM Lookups
                    WHERE [Group] = '" + LookupConstants.GroupTaskStatus + @"'
                    AND Value IN ('" + LookupConstants.ValueTamamlandi + @"', '" + LookupConstants.ValueIptal + @"')
                )",
                commandTimeout: 30);
        });
    }

    /// <summary>Son 24 saatte tamamlanan görev sayısı</summary>
    public async Task<ServiceResult<int>> GetCompletedLast24hCountAsync()
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            return await conn.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*) FROM TaskItems
                WHERE StatusLookupId = (
                    SELECT TOP 1 Id FROM Lookups
                    WHERE [Group] = '" + LookupConstants.GroupTaskStatus + @"'
                    AND Value = '" + LookupConstants.ValueTamamlandi + @"'
                )
                AND CreatedAt >= DATEADD(HOUR, -24, GETUTCDATE())",
                commandTimeout: 30);
        });
    }

    /// <summary>Bugün son tarihli görev sayısı (tamamlanmamış)</summary>
    public async Task<ServiceResult<int>> GetDueTodayCountAsync()
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            return await conn.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*) FROM TaskItems
                WHERE CAST(DueDate AS DATE) = CAST(GETUTCDATE() AS DATE)
                AND StatusLookupId != (
                    SELECT TOP 1 Id FROM Lookups
                    WHERE [Group] = '" + LookupConstants.GroupTaskStatus + @"'
                    AND Value = '" + LookupConstants.ValueTamamlandi + @"'
                )",
                commandTimeout: 30);
        });
    }

    /// <summary>
    /// Son 7 gün içerisindeki görev tamamlanma oranını döner.
    /// </summary>
    public async Task<ServiceResult<int>> GetActionRateAsync()
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            var sql = @"
                SELECT
                    CASE
                        WHEN COUNT(*) = 0 THEN 0
                        ELSE CAST(100.0 * SUM(CASE WHEN StatusLookupId = (
                            SELECT Id FROM Lookups WHERE [Group] = '" + LookupConstants.GroupTaskStatus + @"' AND Value = '" + LookupConstants.ValueTamamlandi + @"'
                        ) THEN 1 ELSE 0 END) / COUNT(*) AS INT)
                    END
                FROM TaskItems
                WHERE CreatedAt >= DATEADD(day, -7, GETUTCDATE())";
            return await conn.ExecuteScalarAsync<int>(sql);
        });
    }
}

