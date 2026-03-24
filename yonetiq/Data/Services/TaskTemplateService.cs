using Dapper;
using Microsoft.Extensions.Configuration;
using YonetIQ.Data.Infrastructure;
using YonetIQ.Data.Models;

namespace YonetIQ.Data.Services;

public class TaskTemplateService(IConfiguration config, AuditService auditSvc)
    : BaseService(config, auditSvc)
{
    public async Task<ServiceResult<List<TaskTemplate>>> ListAsync()
    {
        return await ExecuteServiceAsync<List<TaskTemplate>>(async conn =>
        {
            var templates = await conn.QueryAsync<TaskTemplate>(@"
                SELECT Id, Name, Description, Category, IsActive, CreatedBy, CreatedAt
                FROM TaskTemplates
                ORDER BY Category, Name");
            return templates.ToList();
        });
    }

    public async Task<ServiceResult<TaskTemplate?>> GetWithItemsAsync(int id)
    {
        return await ExecuteServiceAsync<TaskTemplate?>(async conn =>
        {
            var tpl = await conn.QueryFirstOrDefaultAsync<TaskTemplate>(@"
                SELECT Id, Name, Description, Category, IsActive, CreatedBy, CreatedAt
                FROM TaskTemplates WHERE Id = @Id", new { Id = id });
            if (tpl is null) return null;

            var items = await conn.QueryAsync<TaskTemplateItem>(@"
                SELECT Id, TaskTemplateId, Title, Description, Priority, DueDays, OrderIndex, CreatedAt
                FROM TaskTemplateItems
                WHERE TaskTemplateId = @Id
                ORDER BY OrderIndex", new { Id = id });
            tpl.Items = items.ToList();
            return tpl;
        });
    }

    public async Task<ServiceResult<int>> SaveTemplateAsync(TaskTemplate tpl, int userId)
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            int tplId;
            if (tpl.Id == 0)
            {
                tplId = await conn.ExecuteScalarAsync<int>(@"
                    INSERT INTO TaskTemplates (Name, Description, Category, IsActive, CreatedBy, CreatedAt)
                    VALUES (@Name, @Description, @Category, @IsActive, @CreatedBy, GETUTCDATE());
                    SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    new { tpl.Name, tpl.Description, tpl.Category, tpl.IsActive, CreatedBy = userId });
            }
            else
            {
                await conn.ExecuteAsync(@"
                    UPDATE TaskTemplates SET Name=@Name, Description=@Description,
                           Category=@Category, IsActive=@IsActive WHERE Id=@Id",
                    new { tpl.Name, tpl.Description, tpl.Category, tpl.IsActive, tpl.Id });
                tplId = tpl.Id;
                await conn.ExecuteAsync("DELETE FROM TaskTemplateItems WHERE TaskTemplateId=@Id", new { Id = tplId });
            }

            for (int i = 0; i < tpl.Items.Count; i++)
            {
                var item = tpl.Items[i];
                await conn.ExecuteAsync(@"
                    INSERT INTO TaskTemplateItems (TaskTemplateId, Title, Description, Priority, DueDays, OrderIndex, CreatedAt)
                    VALUES (@TaskTemplateId, @Title, @Description, @Priority, @DueDays, @OrderIndex, GETUTCDATE())",
                    new { TaskTemplateId = tplId, item.Title, item.Description, item.Priority, item.DueDays, OrderIndex = i });
            }

            LogAction("TaskTemplate", tpl.Id == 0 ? "Create" : "Update", tpl.Name);
            return tplId;
        });
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int id, int userId)
    {
        return await ExecuteServiceAsync<bool>(async conn =>
        {
            var name = await conn.ExecuteScalarAsync<string>(
                "SELECT Name FROM TaskTemplates WHERE Id=@Id", new { Id = id }) ?? "";
            await conn.ExecuteAsync("DELETE FROM TaskTemplates WHERE Id=@Id", new { Id = id });
            LogAction("TaskTemplate", "Delete", name);
            return true;
        });
    }

    /// <summary>
    /// Şablondan toplu görev oluşturur.
    /// Dapper üzerinden TaskItems tablosuna insert eder.
    /// </summary>
    public async Task<ServiceResult<int>> ApplyTemplateAsync(
        int templateId, int assigneeId, string assigneeName, int statusLookupId, int userId)
    {
        var tplResult = await GetWithItemsAsync(templateId);
        if (!tplResult.IsSuccess || tplResult.Data is null)
            return ServiceResult<int>.Failure("Şablon bulunamadı.");

        var tpl = tplResult.Data;

        return await ExecuteServiceAsync<int>(async conn =>
        {
            var baseDate = DateTime.UtcNow;
            int created = 0;

            foreach (var item in tpl.Items)
            {
                var dueDate = item.DueDays.HasValue
                    ? baseDate.AddDays(item.DueDays.Value)
                    : (DateTime?)null;

                await conn.ExecuteAsync(@"
                    INSERT INTO TaskItems
                        (Title, Description, AssigneeId, AssigneeName, StatusLookupId, AssignedAt,
                         DueDate, Priority, SourceEntityType, SourceEntityId, CreatedAt)
                    VALUES
                        (@Title, @Description, @AssigneeId, @AssigneeName, @StatusLookupId, GETUTCDATE(),
                         @DueDate, @Priority, 'TaskTemplate', @TemplateId, GETUTCDATE())",
                    new
                    {
                        item.Title,
                        item.Description,
                        AssigneeId = assigneeId,
                        AssigneeName = assigneeName,
                        StatusLookupId = statusLookupId,
                        DueDate = dueDate,
                        item.Priority,
                        TemplateId = templateId
                    });
                created++;
            }

            LogAction("TaskTemplate", "Apply", $"{tpl.Name} → {created} görev oluşturuldu");
            return created;
        });
    }
}
