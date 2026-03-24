using Dapper;
using Microsoft.Extensions.Configuration;
using YonetIQ.Data.Infrastructure;
using YonetIQ.Data.Models;

namespace YonetIQ.Data.Services;

public class ApprovalService(IConfiguration config, AuditService auditSvc) : BaseService(config, auditSvc)
{
    public async Task<ServiceResult<List<ApprovalRequest>>> ListAsync(string? status = null)
    {
        return await ExecuteServiceAsync<List<ApprovalRequest>>(async conn =>
        {
            var sql = @"
                SELECT Id, EntityType, EntityId, Title, Description,
                       RequestedBy, RequestedByName, Status, CreatedAt
                FROM ApprovalRequests
                WHERE (@Status IS NULL OR Status = @Status)
                ORDER BY CreatedAt DESC";
            var rows = await conn.QueryAsync<ApprovalRequest>(sql, new { Status = status });
            return rows.ToList();
        });
    }

    public async Task<ServiceResult<List<ApprovalRequest>>> GetPendingForUserAsync(int userId)
    {
        return await ExecuteServiceAsync<List<ApprovalRequest>>(async conn =>
        {
            var rows = await conn.QueryAsync<ApprovalRequest>(@"
                SELECT DISTINCT ar.Id, ar.EntityType, ar.EntityId, ar.Title, ar.Description,
                       ar.RequestedBy, ar.RequestedByName, ar.Status, ar.CreatedAt
                FROM ApprovalRequests ar
                INNER JOIN ApprovalSteps ast ON ast.ApprovalRequestId = ar.Id
                WHERE ar.Status = 'Pending'
                  AND ast.ApproverId = @UserId
                  AND ast.Status = 'Pending'
                  AND ast.StepOrder = (
                      SELECT MIN(s2.StepOrder) FROM ApprovalSteps s2
                      WHERE s2.ApprovalRequestId = ar.Id AND s2.Status = 'Pending'
                  )
                ORDER BY ar.CreatedAt DESC",
                new { UserId = userId });
            return rows.ToList();
        });
    }

    public async Task<ServiceResult<ApprovalRequest?>> GetWithStepsAsync(int id)
    {
        return await ExecuteServiceAsync<ApprovalRequest?>(async conn =>
        {
            var req = await conn.QueryFirstOrDefaultAsync<ApprovalRequest>(@"
                SELECT Id, EntityType, EntityId, Title, Description,
                       RequestedBy, RequestedByName, Status, CreatedAt
                FROM ApprovalRequests WHERE Id = @Id", new { Id = id });
            if (req is null) return null;

            var steps = await conn.QueryAsync<ApprovalStep>(@"
                SELECT Id, ApprovalRequestId, StepOrder, ApproverId, ApproverName,
                       Status, Comment, ActionAt, CreatedAt
                FROM ApprovalSteps WHERE ApprovalRequestId = @Id ORDER BY StepOrder",
                new { Id = id });
            req.Steps = steps.ToList();
            return req;
        });
    }

    public async Task<ServiceResult<int>> CreateAsync(ApprovalRequest request, List<int> approverIds, List<string> approverNames, int userId)
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            var reqId = await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO ApprovalRequests (EntityType, EntityId, Title, Description, RequestedBy, RequestedByName, Status, CreatedAt)
                VALUES (@EntityType, @EntityId, @Title, @Description, @RequestedBy, @RequestedByName, 'Pending', GETUTCDATE());
                SELECT CAST(SCOPE_IDENTITY() AS INT);", request);

            for (int i = 0; i < approverIds.Count; i++)
            {
                await conn.ExecuteAsync(@"
                    INSERT INTO ApprovalSteps (ApprovalRequestId, StepOrder, ApproverId, ApproverName, Status, CreatedAt)
                    VALUES (@ReqId, @Order, @ApproverId, @ApproverName, 'Pending', GETUTCDATE())",
                    new { ReqId = reqId, Order = i + 1, ApproverId = approverIds[i], ApproverName = approverNames[i] });
            }

            LogAction("ApprovalRequest", "Create", request.Title);
            return reqId;
        });
    }

    public async Task<ServiceResult<bool>> ProcessStepAsync(int requestId, int approverId, string action, string? comment)
    {
        // action: "Approved" | "Rejected"
        return await ExecuteServiceAsync<bool>(async conn =>
        {
            var step = await conn.QueryFirstOrDefaultAsync<ApprovalStep>(@"
                SELECT Id, ApprovalRequestId, StepOrder FROM ApprovalSteps
                WHERE ApprovalRequestId = @ReqId AND ApproverId = @ApproverId AND Status = 'Pending'
                ORDER BY StepOrder",
                new { ReqId = requestId, ApproverId = approverId });

            if (step is null)
                throw new InvalidOperationException("Onay adımı bulunamadı.");

            await conn.ExecuteAsync(@"
                UPDATE ApprovalSteps SET Status = @Action, Comment = @Comment, ActionAt = GETUTCDATE()
                WHERE Id = @Id",
                new { Action = action, Comment = comment, Id = step.Id });

            if (action == "Rejected")
            {
                await conn.ExecuteAsync(
                    "UPDATE ApprovalRequests SET Status = 'Rejected' WHERE Id = @Id",
                    new { Id = requestId });
            }
            else
            {
                // Sonraki adım var mı?
                var nextStep = await conn.QueryFirstOrDefaultAsync<int?>(@"
                    SELECT TOP 1 Id FROM ApprovalSteps
                    WHERE ApprovalRequestId = @ReqId AND Status = 'Pending'
                    ORDER BY StepOrder", new { ReqId = requestId });

                if (nextStep is null)
                {
                    await conn.ExecuteAsync(
                        "UPDATE ApprovalRequests SET Status = 'Approved' WHERE Id = @Id",
                        new { Id = requestId });
                }
            }

            LogAction("ApprovalStep", action, $"Talep #{requestId}");
            return true;
        });
    }

    /// <summary>Kullanıcının bekleyen onay sayısını döner</summary>
    public async Task<ServiceResult<int>> GetPendingApprovalCountAsync(int userId)
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            return await conn.ExecuteScalarAsync<int>(@"
                SELECT COUNT(DISTINCT ar.Id)
                FROM ApprovalRequests ar
                INNER JOIN ApprovalSteps ast ON ast.ApprovalRequestId = ar.Id
                WHERE ar.Status = 'Pending'
                AND ast.ApproverId = @userId
                AND ast.Status = 'Pending'",
                new { userId },
                commandTimeout: 30);
        });
    }

    public async Task<ServiceResult<bool>> CancelAsync(int requestId, int userId)
    {
        return await ExecuteServiceAsync<bool>(async conn =>
        {
            await conn.ExecuteAsync(
                "UPDATE ApprovalRequests SET Status = 'Cancelled' WHERE Id = @Id AND RequestedBy = @UserId",
                new { Id = requestId, UserId = userId });
            LogAction("ApprovalRequest", "Cancel", $"#{requestId}");
            return true;
        });
    }
}
