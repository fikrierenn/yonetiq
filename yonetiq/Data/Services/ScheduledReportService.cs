using Dapper;
using Microsoft.Extensions.Configuration;
using YonetIQ.Data.Infrastructure;
using YonetIQ.Data.Models;

namespace YonetIQ.Data.Services;

/// <summary>
/// Zamanlanmış rapor CRUD ve zamanlama hesaplama işlemlerini yöneten servis.
/// </summary>
public class ScheduledReportService(IConfiguration config, AuditService auditSvc)
    : BaseService(config, auditSvc)
{
    public async Task<ServiceResult<List<ScheduledReport>>> ListAsync()
    {
        return await ExecuteServiceAsync<List<ScheduledReport>>(async conn =>
        {
            var rows = await conn.QueryAsync<ScheduledReport>(@"
                SELECT sr.Id, sr.QueryRecordId, sr.Title, sr.Channel, sr.Recipient,
                       sr.FrequencyType, sr.RunTime, sr.DayOfWeek, sr.DayOfMonth,
                       sr.IsActive, sr.LastRunAt, sr.NextRunAt, sr.CreatedBy, sr.CreatedAt,
                       qr.Name AS QueryName
                FROM ScheduledReports sr
                LEFT JOIN QueryRecords qr ON qr.Id = sr.QueryRecordId
                ORDER BY sr.Title");
            return rows.ToList();
        });
    }

    public async Task<ServiceResult<ScheduledReport?>> GetAsync(int id)
    {
        return await ExecuteServiceAsync<ScheduledReport?>(async conn =>
        {
            return await conn.QueryFirstOrDefaultAsync<ScheduledReport>(@"
                SELECT sr.Id, sr.QueryRecordId, sr.Title, sr.Channel, sr.Recipient,
                       sr.FrequencyType, sr.RunTime, sr.DayOfWeek, sr.DayOfMonth,
                       sr.IsActive, sr.LastRunAt, sr.NextRunAt, sr.CreatedBy, sr.CreatedAt,
                       qr.Name AS QueryName
                FROM ScheduledReports sr
                LEFT JOIN QueryRecords qr ON qr.Id = sr.QueryRecordId
                WHERE sr.Id = @Id", new { Id = id });
        });
    }

    public async Task<ServiceResult<int>> SaveAsync(ScheduledReport sr, int userId)
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            if (sr.Id == 0)
            {
                sr.NextRunAt = ComputeNextRun(sr);
                var newId = await conn.ExecuteScalarAsync<int>(@"
                    INSERT INTO ScheduledReports
                        (QueryRecordId, Title, Channel, Recipient, FrequencyType, RunTime,
                         DayOfWeek, DayOfMonth, IsActive, NextRunAt, CreatedBy, CreatedAt)
                    VALUES
                        (@QueryRecordId, @Title, @Channel, @Recipient, @FrequencyType, @RunTime,
                         @DayOfWeek, @DayOfMonth, @IsActive, @NextRunAt, @CreatedBy, GETUTCDATE());
                    SELECT CAST(SCOPE_IDENTITY() AS INT);", sr);
                LogAction("ScheduledReport", "Create", new { Title = sr.Title, UserId = userId });
                return newId;
            }
            else
            {
                sr.NextRunAt = ComputeNextRun(sr);
                await conn.ExecuteAsync(@"
                    UPDATE ScheduledReports SET
                        QueryRecordId = @QueryRecordId, Title = @Title, Channel = @Channel,
                        Recipient = @Recipient, FrequencyType = @FrequencyType, RunTime = @RunTime,
                        DayOfWeek = @DayOfWeek, DayOfMonth = @DayOfMonth,
                        IsActive = @IsActive, NextRunAt = @NextRunAt, UpdatedAt = GETUTCDATE()
                    WHERE Id = @Id", sr);
                LogAction("ScheduledReport", "Update", new { Title = sr.Title, UserId = userId });
                return sr.Id;
            }
        });
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int id, int userId)
    {
        return await ExecuteServiceAsync<bool>(async conn =>
        {
            var title = await conn.ExecuteScalarAsync<string>(
                "SELECT Title FROM ScheduledReports WHERE Id = @Id", new { Id = id }) ?? "";
            await conn.ExecuteAsync("DELETE FROM ScheduledReports WHERE Id = @Id", new { Id = id });
            LogAction("ScheduledReport", "Delete", new { Title = title, UserId = userId });
            return true;
        });
    }

    public async Task<ServiceResult<List<ScheduledReport>>> GetDueReportsAsync()
    {
        return await ExecuteServiceAsync<List<ScheduledReport>>(async conn =>
        {
            var rows = await conn.QueryAsync<ScheduledReport>(@"
                SELECT sr.Id, sr.QueryRecordId, sr.Title, sr.Channel, sr.Recipient,
                       sr.FrequencyType, sr.RunTime, sr.DayOfWeek, sr.DayOfMonth,
                       sr.IsActive, sr.LastRunAt, sr.NextRunAt, sr.CreatedBy, sr.CreatedAt
                FROM ScheduledReports sr
                WHERE sr.IsActive = 1
                  AND sr.NextRunAt IS NOT NULL
                  AND sr.NextRunAt <= GETUTCDATE()");
            return rows.ToList();
        });
    }

    public async Task<ServiceResult<bool>> UpdateAfterRunAsync(int id)
    {
        return await ExecuteServiceAsync<bool>(async conn =>
        {
            var sr = await conn.QueryFirstOrDefaultAsync<ScheduledReport>(
                "SELECT Id, FrequencyType, RunTime, DayOfWeek, DayOfMonth FROM ScheduledReports WHERE Id = @Id",
                new { Id = id });
            if (sr is null) return false;
            var next = ComputeNextRun(sr);
            await conn.ExecuteAsync(
                "UPDATE ScheduledReports SET LastRunAt = GETUTCDATE(), NextRunAt = @Next, UpdatedAt = GETUTCDATE() WHERE Id = @Id",
                new { Id = id, Next = next });
            return true;
        });
    }

    private static DateTime ComputeNextRun(ScheduledReport sr)
    {
        var now = DateTime.UtcNow;
        var todayRun = DateTime.UtcNow.Date.Add(sr.RunTime.ToTimeSpan());
        return sr.FrequencyType switch
        {
            "Weekly"  when sr.DayOfWeek.HasValue  => GetNextWeekday(now, sr.DayOfWeek.Value, sr.RunTime),
            "Monthly" when sr.DayOfMonth.HasValue => GetNextMonthDay(now, sr.DayOfMonth.Value, sr.RunTime),
            _ => todayRun > now ? todayRun : todayRun.AddDays(1)  // Daily
        };
    }

    private static DateTime GetNextWeekday(DateTime from, int dayOfWeek, TimeOnly time)
    {
        // dayOfWeek: 1=Mon..7=Sun (ISO)
        var dotNetDow = dayOfWeek == 7 ? DayOfWeek.Sunday : (DayOfWeek)dayOfWeek;
        var daysUntil = ((int)dotNetDow - (int)from.DayOfWeek + 7) % 7;
        if (daysUntil == 0 && from.TimeOfDay >= time.ToTimeSpan()) daysUntil = 7;
        return from.Date.AddDays(daysUntil).Add(time.ToTimeSpan());
    }

    private static DateTime GetNextMonthDay(DateTime from, int day, TimeOnly time)
    {
        var candidate = new DateTime(from.Year, from.Month,
                Math.Min(day, DateTime.DaysInMonth(from.Year, from.Month)))
            .Add(time.ToTimeSpan());
        if (candidate <= from) candidate = candidate.AddMonths(1);
        return candidate;
    }
}
