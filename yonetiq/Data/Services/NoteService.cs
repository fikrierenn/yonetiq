using Dapper;
using Microsoft.Data.SqlClient;
using YonetIQ.Data.Models;
using YonetIQ.Data.Infrastructure;

namespace YonetIQ.Data.Services;

/// <summary>
/// Kişisel not yönetimi: oluşturma, düzenleme, silme, hatırlatma ve göreve dönüştürme.
/// </summary>
public class NoteService(IConfiguration config, AuditService auditService, TaskService taskSvc) : BaseService(config, auditService)
{
    public async Task<ServiceResult<List<PersonalNote>>> ListAsync(int userId, string? tag = null)
    {
        return await ExecuteServiceAsync<List<PersonalNote>>(async conn =>
        {
            var sql = """
                SELECT Id, UserId, Title, Content, Tags, ReminderAt, IsReminderDismissed, LinkedTaskId, CreatedAt
                FROM PersonalNotes
                WHERE UserId = @UserId
                ORDER BY ReminderAt ASC, CreatedAt DESC
                """;
            var notes = (await conn.QueryAsync<PersonalNote>(sql, new { UserId = userId })).ToList();

            if (!string.IsNullOrWhiteSpace(tag))
            {
                notes = notes
                    .Where(n => n.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Any(t => t.Trim().Equals(tag.Trim(), StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            return notes;
        });
    }

    public async Task<ServiceResult<PersonalNote>> GetAsync(int id)
    {
        try
        {
            await using var conn = CreateConn();
            await conn.OpenAsync();
            var note = await conn.QueryFirstOrDefaultAsync<PersonalNote>(
                "SELECT Id, UserId, Title, Content, Tags, ReminderAt, IsReminderDismissed, LinkedTaskId, CreatedAt FROM PersonalNotes WHERE Id = @Id",
                new { Id = id });
            return note is not null
                ? ServiceResult<PersonalNote>.Success(note)
                : ServiceResult<PersonalNote>.Failure("Not bulunamadı.");
        }
        catch (SqlException ex) { return ServiceResult<PersonalNote>.Failure($"Veritabani hatasi: {ex.Message}", ex.Number.ToString()); }
        catch (Exception ex)    { return ServiceResult<PersonalNote>.Failure($"Sistem hatasi: {ex.Message}"); }
    }

    public async Task<ServiceResult<int>> SaveAsync(PersonalNote note)
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            if (note.Id == 0)
            {
                var id = await conn.ExecuteScalarAsync<int>("""
                    INSERT INTO PersonalNotes (UserId, Title, Content, Tags, ReminderAt, IsReminderDismissed, LinkedTaskId, CreatedAt)
                    OUTPUT INSERTED.Id
                    VALUES (@UserId, @Title, @Content, @Tags, @ReminderAt, @IsReminderDismissed, @LinkedTaskId, @CreatedAt)
                    """, note);
                LogAction("Note", "Create", new { NoteId = id, Title = note.Title });
                return id;
            }
            else
            {
                await conn.ExecuteAsync("""
                    UPDATE PersonalNotes SET
                        Title = @Title,
                        Content = @Content,
                        Tags = @Tags,
                        ReminderAt = @ReminderAt,
                        IsReminderDismissed = @IsReminderDismissed,
                        LinkedTaskId = @LinkedTaskId
                    WHERE Id = @Id AND UserId = @UserId
                    """, note);
                LogAction("Note", "Update", new { NoteId = note.Id, Title = note.Title });
                return note.Id;
            }
        });
    }

    public async Task<ServiceResult> DeleteAsync(int id, int userId)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            await conn.ExecuteAsync(
                "DELETE FROM PersonalNotes WHERE Id = @Id AND UserId = @UserId",
                new { Id = id, UserId = userId });
            LogAction("Note", "Delete", new { NoteId = id });
        });
    }

    public async Task<ServiceResult> DismissReminderAsync(int id, int userId)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            await conn.ExecuteAsync(
                "UPDATE PersonalNotes SET IsReminderDismissed = 1 WHERE Id = @Id AND UserId = @UserId",
                new { Id = id, UserId = userId });
        });
    }

    /// <summary>
    /// Hatırlatma zamanı geçmiş (son 24 saat) ve henüz kapatılmamış notları döner.
    /// </summary>
    public async Task<ServiceResult<List<PersonalNote>>> GetDueRemindersAsync(int userId)
    {
        return await ExecuteServiceAsync<List<PersonalNote>>(async conn =>
        {
            var notes = (await conn.QueryAsync<PersonalNote>("""
                SELECT * FROM PersonalNotes
                WHERE UserId = @UserId
                  AND IsReminderDismissed = 0
                  AND ReminderAt IS NOT NULL
                  AND ReminderAt <= SYSUTCDATETIME()
                  AND ReminderAt >= DATEADD(HOUR, -24, SYSUTCDATETIME())
                ORDER BY ReminderAt ASC
                """, new { UserId = userId })).ToList();
            return notes;
        });
    }

    /// <summary>
    /// Kişisel notu göreve dönüştürür ve LinkedTaskId'yi set eder.
    /// </summary>
    public async Task<ServiceResult<int>> ConvertToTaskAsync(int noteId, int userId, TaskItem task)
    {
        // Önce görevi kaydet — sub-servis hataları doğrudan yayılır
        var saveRes = await taskSvc.SaveAsync(task);
        if (!saveRes.IsSuccess)
            return ServiceResult<int>.Failure(saveRes.Message);

        var taskId = saveRes.Data;

        // Ardından not'un LinkedTaskId'sini güncelle
        return await ExecuteServiceAsync<int>(async conn =>
        {
            await conn.ExecuteAsync(
                "UPDATE PersonalNotes SET LinkedTaskId = @TaskId WHERE Id = @NoteId AND UserId = @UserId",
                new { TaskId = taskId, NoteId = noteId, UserId = userId });

            LogAction("Note", "ConvertToTask", new { NoteId = noteId, TaskId = taskId });
            return taskId;
        });
    }
}
