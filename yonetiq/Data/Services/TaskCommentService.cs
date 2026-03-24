using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using YonetIQ.Data.Infrastructure;
using YonetIQ.Data.Models;

namespace YonetIQ.Data.Services;

/// <summary>
/// Görev yorumları ve aktivite akışı yönetimi.
/// </summary>
public class TaskCommentService(IConfiguration config, AuditService auditSvc) : BaseService(config, auditSvc)
{
    // ── Yorumlar ────────────────────────────────────────────────────

    public async Task<ServiceResult<List<TaskComment>>> GetCommentsAsync(int taskItemId)
    {
        return await ExecuteServiceAsync<List<TaskComment>>(async conn =>
        {
            var items = await conn.QueryAsync<TaskComment>(@"
                SELECT Id, TaskItemId, UserId, UserName, Content, CreatedAt
                FROM TaskComments
                WHERE TaskItemId = @TaskItemId
                ORDER BY CreatedAt ASC", new { TaskItemId = taskItemId });
            return items.ToList();
        });
    }

    public async Task<ServiceResult<int>> AddCommentAsync(int taskItemId, int userId, string userName, string content)
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            var id = await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO TaskComments (TaskItemId, UserId, UserName, Content, CreatedAt)
                VALUES (@TaskItemId, @UserId, @UserName, @Content, GETUTCDATE());
                SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new { TaskItemId = taskItemId, UserId = userId, UserName = userName, Content = content });
            LogAction("TaskComment", "Add", new { TaskItemId = taskItemId, UserId = userId });
            return id;
        });
    }

    public async Task<ServiceResult<bool>> DeleteCommentAsync(int commentId, int requestingUserId)
    {
        return await ExecuteServiceAsync<bool>(async conn =>
        {
            var affected = await conn.ExecuteAsync(
                "DELETE FROM TaskComments WHERE Id = @Id AND UserId = @UserId",
                new { Id = commentId, UserId = requestingUserId });
            return affected > 0;
        });
    }

    // ── Aktivite Akışı ──────────────────────────────────────────────

    public async Task<ServiceResult<List<TaskActivity>>> GetActivityAsync(int taskItemId)
    {
        return await ExecuteServiceAsync<List<TaskActivity>>(async conn =>
        {
            var items = await conn.QueryAsync<TaskActivity>(@"
                SELECT Id, TaskItemId, UserId, UserName, ActionType, OldValue, NewValue, Note, CreatedAt
                FROM TaskActivity
                WHERE TaskItemId = @TaskItemId
                ORDER BY CreatedAt DESC", new { TaskItemId = taskItemId });
            return items.ToList();
        });
    }

    public async Task LogActivityAsync(SqlConnection conn, int taskItemId, int userId, string userName,
        string actionType, string? oldValue = null, string? newValue = null, string? note = null)
    {
        await conn.ExecuteAsync(@"
            INSERT INTO TaskActivity (TaskItemId, UserId, UserName, ActionType, OldValue, NewValue, Note, CreatedAt)
            VALUES (@TaskItemId, @UserId, @UserName, @ActionType, @OldValue, @NewValue, @Note, GETUTCDATE())",
            new { TaskItemId = taskItemId, UserId = userId, UserName = userName,
                  ActionType = actionType, OldValue = oldValue, NewValue = newValue, Note = note });
    }
}
