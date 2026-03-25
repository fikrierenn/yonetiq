using Dapper;
using Microsoft.Extensions.Configuration;
using YonetIQ.Data.Infrastructure;
using YonetIQ.Data.Models;

namespace YonetIQ.Data.Services;

public class KpiTargetService(IConfiguration config, AuditService auditSvc) : BaseService(config, auditSvc)
{
    public async Task<ServiceResult<List<KpiTarget>>> ListAsync()
    {
        return await ExecuteServiceAsync<List<KpiTarget>>(async conn =>
        {
            var rows = await conn.QueryAsync<KpiTarget>(@"
                SELECT Id, MetricKey, Label, TargetValue, Operator, PeriodType, IsActive, CreatedAt
                FROM KpiTargets
                WHERE IsActive = 1
                ORDER BY Label");
            return rows.ToList();
        });
    }

    public async Task<ServiceResult<List<KpiTarget>>> ListAllAsync()
    {
        return await ExecuteServiceAsync<List<KpiTarget>>(async conn =>
        {
            var rows = await conn.QueryAsync<KpiTarget>(@"
                SELECT Id, MetricKey, Label, TargetValue, Operator, PeriodType, IsActive, CreatedAt
                FROM KpiTargets
                ORDER BY Label");
            return rows.ToList();
        });
    }

    public async Task<ServiceResult<int>> SaveAsync(KpiTarget target, int userId)
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            if (target.Id == 0)
            {
                var newId = await conn.ExecuteScalarAsync<int>(@"
                    INSERT INTO KpiTargets (MetricKey, Label, TargetValue, Operator, PeriodType, IsActive, CreatedAt)
                    VALUES (@MetricKey, @Label, @TargetValue, @Operator, @PeriodType, @IsActive, GETUTCDATE());
                    SELECT CAST(SCOPE_IDENTITY() AS INT);", target);
                LogAction("KpiTarget", "Create", new { UserId = userId, Label = target.Label });
                return newId;
            }
            else
            {
                await conn.ExecuteAsync(@"
                    UPDATE KpiTargets SET
                        MetricKey   = @MetricKey,
                        Label       = @Label,
                        TargetValue = @TargetValue,
                        Operator    = @Operator,
                        PeriodType  = @PeriodType,
                        IsActive    = @IsActive,
                        UpdatedAt   = GETUTCDATE()
                    WHERE Id = @Id", target);
                LogAction("KpiTarget", "Update", new { UserId = userId, Label = target.Label, Id = target.Id });
                return target.Id;
            }
        });
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int id, int userId)
    {
        return await ExecuteServiceAsync<bool>(async conn =>
        {
            var label = await conn.ExecuteScalarAsync<string>(
                "SELECT Label FROM KpiTargets WHERE Id = @Id", new { Id = id }) ?? "";
            await conn.ExecuteAsync("DELETE FROM KpiTargets WHERE Id = @Id", new { Id = id });
            LogAction("KpiTarget", "Delete", new { UserId = userId, Id = id, Label = label });
            return true;
        });
    }
}
