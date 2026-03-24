using Dapper;
using Microsoft.Extensions.Configuration;
using YonetIQ.Data.Infrastructure;
using YonetIQ.Data.Models;

namespace YonetIQ.Data.Services;

public class TimeTrackingService(IConfiguration config, AuditService auditSvc) : BaseService(config, auditSvc)
{
    /// <summary>Görev için tüm zaman kayıtlarını döner.</summary>
    public async Task<ServiceResult<List<TimeEntry>>> GetByTaskAsync(int taskItemId)
    {
        return await ExecuteServiceAsync<List<TimeEntry>>(async conn =>
        {
            var rows = await conn.QueryAsync<TimeEntry>(@"
                SELECT Id, TaskItemId, UserId, UserName, StartedAt, EndedAt, DurationMin, Note, CreatedAt
                FROM TimeEntries
                WHERE TaskItemId = @TaskItemId
                ORDER BY StartedAt DESC", new { TaskItemId = taskItemId });
            return rows.ToList();
        });
    }

    /// <summary>Kullanıcı bazlı efor raporu.</summary>
    public async Task<ServiceResult<List<TimeEntry>>> GetByUserAsync(int userId, DateTime? from = null, DateTime? to = null)
    {
        return await ExecuteServiceAsync<List<TimeEntry>>(async conn =>
        {
            var sql = @"
                SELECT te.Id, te.TaskItemId, te.UserId, te.UserName, te.StartedAt, te.EndedAt, te.DurationMin, te.Note, te.CreatedAt
                FROM TimeEntries te
                WHERE te.UserId = @UserId
                  AND (@From IS NULL OR te.StartedAt >= @From)
                  AND (@To IS NULL OR te.StartedAt <= @To)
                ORDER BY te.StartedAt DESC";
            var rows = await conn.QueryAsync<TimeEntry>(sql, new { UserId = userId, From = from, To = to });
            return rows.ToList();
        });
    }

    /// <summary>Tüm kullanıcıların kayıtlarını döner (filtreli).</summary>
    public async Task<ServiceResult<List<TimeEntry>>> GetAllAsync(DateTime? from = null, DateTime? to = null)
    {
        return await ExecuteServiceAsync<List<TimeEntry>>(async conn =>
        {
            var sql = @"
                SELECT te.Id, te.TaskItemId, te.UserId, te.UserName, te.StartedAt, te.EndedAt, te.DurationMin, te.Note, te.CreatedAt
                FROM TimeEntries te
                WHERE (@From IS NULL OR te.StartedAt >= @From)
                  AND (@To IS NULL OR te.StartedAt <= @To)
                ORDER BY te.StartedAt DESC";
            var rows = await conn.QueryAsync<TimeEntry>(sql, new { From = from, To = to });
            return rows.ToList();
        });
    }

    /// <summary>Manuel zaman kaydı ekler (başlangıç + bitiş verilmiş).</summary>
    public async Task<ServiceResult<int>> AddManualAsync(int taskItemId, int userId, string userName,
        DateTime start, DateTime end, string? note)
    {
        if (end <= start)
            return ServiceResult<int>.Failure("Bitiş zamanı başlangıçtan önce olamaz.");

        var durationMin = (int)(end - start).TotalMinutes;
        return await ExecuteServiceAsync<int>(async conn =>
        {
            var id = await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO TimeEntries (TaskItemId, UserId, UserName, StartedAt, EndedAt, DurationMin, Note, CreatedAt)
                VALUES (@TaskItemId, @UserId, @UserName, @Start, @End, @Duration, @Note, GETUTCDATE());
                SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new { TaskItemId = taskItemId, UserId = userId, UserName = userName,
                      Start = start, End = end, Duration = durationMin, Note = note });
            LogAction("TimeTracking", "AddManual", new { taskItemId, userId, durationMin });
            return id;
        });
    }

    /// <summary>Timer başlat — aktif devam eden kayıt oluştur.</summary>
    public async Task<ServiceResult<int>> StartTimerAsync(int taskItemId, int userId, string userName)
    {
        // Önce devam eden kayıt kontrolü
        var runningRes = await GetRunningTimerAsync(taskItemId, userId);
        if (runningRes.IsSuccess && runningRes.Data is not null)
            return ServiceResult<int>.Failure("Zaten çalışan bir timer var.");

        return await ExecuteServiceAsync<int>(async conn =>
        {
            var id = await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO TimeEntries (TaskItemId, UserId, UserName, StartedAt, CreatedAt)
                VALUES (@TaskItemId, @UserId, @UserName, GETUTCDATE(), GETUTCDATE());
                SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new { TaskItemId = taskItemId, UserId = userId, UserName = userName });
            LogAction("TimeTracking", "StartTimer", new { taskItemId, userId });
            return id;
        });
    }

    /// <summary>Timer durdur — EndedAt ve DurationMin günceller.</summary>
    public async Task<ServiceResult<TimeEntry?>> StopTimerAsync(int entryId, string? note)
    {
        // Önce kaydın var olduğunu kontrol et
        var checkRes = await ExecuteServiceAsync<TimeEntry?>(async conn =>
        {
            return await conn.QueryFirstOrDefaultAsync<TimeEntry>(
                "SELECT Id, TaskItemId, UserId, UserName, StartedAt, EndedAt, DurationMin, Note, CreatedAt FROM TimeEntries WHERE Id = @Id AND EndedAt IS NULL",
                new { Id = entryId });
        });

        if (!checkRes.IsSuccess) return checkRes;
        if (checkRes.Data is null)
            return ServiceResult<TimeEntry?>.Failure("Timer bulunamadı veya zaten durdurulmuş.");

        var entry = checkRes.Data;
        var now = DateTime.UtcNow;
        var duration = (int)(now - entry.StartedAt).TotalMinutes;

        var updateRes = await ExecuteServiceAsync<bool>(async conn =>
        {
            await conn.ExecuteAsync(@"
                UPDATE TimeEntries SET EndedAt = @Now, DurationMin = @Duration, Note = @Note WHERE Id = @Id",
                new { Now = now, Duration = duration, Note = note, Id = entryId });
            return true;
        });

        if (!updateRes.IsSuccess)
            return ServiceResult<TimeEntry?>.Failure(updateRes.Message);

        entry.EndedAt = now;
        entry.DurationMin = duration;
        LogAction("TimeTracking", "StopTimer", new { entryId, duration });
        return ServiceResult<TimeEntry?>.Success(entry);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int id, int userId)
    {
        return await ExecuteServiceAsync<bool>(async conn =>
        {
            await conn.ExecuteAsync("DELETE FROM TimeEntries WHERE Id = @Id AND UserId = @UserId",
                new { Id = id, UserId = userId });
            LogAction("TimeTracking", "Delete", new { id, userId });
            return true;
        });
    }

    /// <summary>Görev bazlı toplam efor (dakika).</summary>
    public async Task<ServiceResult<int>> GetTotalMinutesAsync(int taskItemId)
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            var total = await conn.ExecuteScalarAsync<int?>(
                "SELECT ISNULL(SUM(DurationMin), 0) FROM TimeEntries WHERE TaskItemId = @Id AND DurationMin IS NOT NULL",
                new { Id = taskItemId });
            return total ?? 0;
        });
    }

    /// <summary>Görev için devam eden (aktif) timer kaydını döner.</summary>
    public async Task<ServiceResult<TimeEntry?>> GetRunningTimerAsync(int taskItemId, int userId)
    {
        return await ExecuteServiceAsync<TimeEntry?>(async conn =>
        {
            var entry = await conn.QueryFirstOrDefaultAsync<TimeEntry>(@"
                SELECT Id, TaskItemId, UserId, UserName, StartedAt, EndedAt, DurationMin, Note, CreatedAt
                FROM TimeEntries
                WHERE TaskItemId = @TaskItemId AND UserId = @UserId AND EndedAt IS NULL",
                new { TaskItemId = taskItemId, UserId = userId });
            return entry;
        });
    }
}
