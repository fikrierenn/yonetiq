using Dapper;
using Microsoft.Extensions.Configuration;
using YonetIQ.Data.Infrastructure;
using YonetIQ.Data.Models;

namespace YonetIQ.Data.Services;

public class OkrService(IConfiguration config, AuditService auditSvc) : BaseService(config, auditSvc)
{
    public async Task<ServiceResult<List<Objective>>> ListAsync(int? year = null, string? period = null, string? level = null)
    {
        return await ExecuteServiceAsync<List<Objective>>(async conn =>
        {
            var objectives = await conn.QueryAsync<Objective>(@"
                SELECT Id, Title, Description, OwnerId, OwnerName, Level, Period, Year,
                       Status, Progress, CreatedAt
                FROM Objectives
                WHERE (@Year IS NULL OR Year = @Year)
                  AND (@Period IS NULL OR Period = @Period)
                  AND (@Level IS NULL OR Level = @Level)
                  AND Status != 'Cancelled'
                ORDER BY Level, Period, Title",
                new { Year = year, Period = period, Level = level });
            return objectives.ToList();
        });
    }

    public async Task<ServiceResult<Objective?>> GetWithKeyResultsAsync(int id)
    {
        return await ExecuteServiceAsync<Objective?>(async conn =>
        {
            var obj = await conn.QueryFirstOrDefaultAsync<Objective>(@"
                SELECT Id, Title, Description, OwnerId, OwnerName, Level, Period, Year,
                       Status, Progress, CreatedAt
                FROM Objectives WHERE Id = @Id", new { Id = id });
            if (obj is null) return null;

            var krs = await conn.QueryAsync<KeyResult>(@"
                SELECT Id, ObjectiveId, Title, MetricType, StartValue, TargetValue,
                       CurrentValue, Unit, OwnerId, OwnerName, LinkedTaskId, Progress, CreatedAt
                FROM KeyResults WHERE ObjectiveId = @Id ORDER BY Id", new { Id = id });
            obj.KeyResults = krs.ToList();
            return obj;
        });
    }

    public async Task<ServiceResult<int>> SaveObjectiveAsync(Objective obj, int userId)
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            int objId;
            if (obj.Id == 0)
            {
                objId = await conn.ExecuteScalarAsync<int>(@"
                    INSERT INTO Objectives (Title, Description, OwnerId, OwnerName, Level, Period, Year, Status, Progress, CreatedAt)
                    VALUES (@Title, @Description, @OwnerId, @OwnerName, @Level, @Period, @Year, @Status, 0, GETUTCDATE());
                    SELECT CAST(SCOPE_IDENTITY() AS INT);", obj);
                LogAction("OKR", "Create", obj.Title);
            }
            else
            {
                await conn.ExecuteAsync(@"
                    UPDATE Objectives SET Title=@Title, Description=@Description, OwnerId=@OwnerId,
                           OwnerName=@OwnerName, Level=@Level, Period=@Period, Year=@Year, Status=@Status,
                           UpdatedAt=GETUTCDATE()
                    WHERE Id=@Id", obj);
                objId = obj.Id;
                LogAction("OKR", "Update", obj.Title);
            }
            return objId;
        });
    }

    public async Task<ServiceResult<bool>> UpdateKeyResultAsync(int krId, decimal currentValue, int userId)
    {
        return await ExecuteServiceAsync<bool>(async conn =>
        {
            var kr = await conn.QueryFirstOrDefaultAsync<KeyResult>(
                "SELECT Id, ObjectiveId, StartValue, TargetValue, MetricType FROM KeyResults WHERE Id = @Id",
                new { Id = krId });
            if (kr is null) return false;

            decimal progress;
            if (kr.MetricType == "Boolean")
                progress = currentValue > 0 ? 100 : 0;
            else
            {
                var range = kr.TargetValue - kr.StartValue;
                progress = range == 0 ? 0 : Math.Min(100, Math.Max(0, (currentValue - kr.StartValue) / range * 100));
            }

            await conn.ExecuteAsync(
                "UPDATE KeyResults SET CurrentValue = @Val, Progress = @Prog, UpdatedAt = GETUTCDATE() WHERE Id = @Id",
                new { Val = currentValue, Prog = progress, Id = krId });

            // Objective progress'i güncelle (KR'ların ortalaması)
            var avgProg = await conn.ExecuteScalarAsync<decimal>(
                "SELECT AVG(Progress) FROM KeyResults WHERE ObjectiveId = @ObjId",
                new { ObjId = kr.ObjectiveId });
            await conn.ExecuteAsync(
                "UPDATE Objectives SET Progress = @P, UpdatedAt = GETUTCDATE() WHERE Id = @Id",
                new { P = avgProg, Id = kr.ObjectiveId });

            return true;
        });
    }

    public async Task<ServiceResult<int>> SaveKeyResultAsync(KeyResult kr, int userId)
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            if (kr.Id == 0)
            {
                var newId = await conn.ExecuteScalarAsync<int>(@"
                    INSERT INTO KeyResults (ObjectiveId, Title, MetricType, StartValue, TargetValue,
                                            CurrentValue, Unit, OwnerId, OwnerName, LinkedTaskId, Progress, CreatedAt)
                    VALUES (@ObjectiveId, @Title, @MetricType, @StartValue, @TargetValue,
                            @CurrentValue, @Unit, @OwnerId, @OwnerName, @LinkedTaskId, 0, GETUTCDATE());
                    SELECT CAST(SCOPE_IDENTITY() AS INT);", kr);
                return newId;
            }
            else
            {
                await conn.ExecuteAsync(@"
                    UPDATE KeyResults SET Title=@Title, MetricType=@MetricType, StartValue=@StartValue,
                           TargetValue=@TargetValue, Unit=@Unit, OwnerId=@OwnerId, OwnerName=@OwnerName,
                           LinkedTaskId=@LinkedTaskId, UpdatedAt=GETUTCDATE() WHERE Id=@Id", kr);
                return kr.Id;
            }
        });
    }

    public async Task<ServiceResult<bool>> DeleteObjectiveAsync(int id, int userId)
    {
        return await ExecuteServiceAsync<bool>(async conn =>
        {
            var title = await conn.ExecuteScalarAsync<string>(
                "SELECT Title FROM Objectives WHERE Id = @Id", new { Id = id }) ?? "";
            await conn.ExecuteAsync("DELETE FROM Objectives WHERE Id = @Id", new { Id = id });
            LogAction("OKR", "Delete", title);
            return true;
        });
    }
}
