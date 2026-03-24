using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using YonetIQ.Data.Models;
using YonetIQ.Data.Infrastructure;

namespace YonetIQ.Data.Services;

/// <summary>
/// Takvim ve ozel gun islemlerini yoneten servis sinifidir.
/// </summary>
public class CalendarService(IConfiguration config) : BaseService(config)
{
    /// <summary>
    /// Belirli bir tarih araligindaki takvim olaylarini ve ozel gunleri listeler.
    /// </summary>
    public async Task<ServiceResult<List<CalendarEvent>>> ListAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        return await ExecuteServiceAsync<List<CalendarEvent>>(async conn =>
        {
            var start = (startDate ?? DateTime.Today.AddMonths(-1)).Date;
            var end = (endDate ?? DateTime.Today.AddMonths(1)).Date;

            List<CalendarEvent> mainEvents;
            try
            {
                mainEvents = (await conn.QueryAsync<CalendarEvent>(
                    "sp_Calendar_ListAsEvents",
                    new { StartDate = start, EndDate = end },
                    commandType: System.Data.CommandType.StoredProcedure)).ToList();
            }
            catch (SqlException ex) when (IsCompatibleWithFallback(ex))
            {
                var sql = @"
                    SELECT
                        MeetingDate                        AS Date,
                        N'Meeting'                         AS Type,
                        Title                              AS Title,
                        ISNULL(Location, N'')              AS Detail,
                        Id                                 AS ReferenceId,
                        MeetingLink                        AS Link,
                        2                                  AS Priority
                    FROM Meetings
                    WHERE (@StartDate IS NULL OR CAST(MeetingDate AS DATE) >= @StartDate)
                      AND (@EndDate   IS NULL OR CAST(MeetingDate AS DATE) <= @EndDate)

                    UNION ALL

                    SELECT
                        CAST(DueDate AS DATETIME2)         AS Date,
                        N'TaskItem'                        AS Type,
                        Title                              AS Title,
                        ISNULL(Description, N'')           AS Detail,
                        Id                                 AS ReferenceId,
                        NULL                               AS Link,
                        3                                  AS Priority
                    FROM TaskItems
                    WHERE DueDate IS NOT NULL
                      AND (@StartDate IS NULL OR CAST(DueDate AS DATE) >= @StartDate)
                      AND (@EndDate   IS NULL OR CAST(DueDate AS DATE) <= @EndDate)

                    ORDER BY Date, Priority, Title";
                mainEvents = (await conn.QueryAsync<CalendarEvent>(
                    sql, new { StartDate = start, EndDate = end })).ToList();
            }

            List<CalendarEvent> specialDayEvents;
            try
            {
                specialDayEvents = (await conn.QueryAsync<CalendarEvent>(
                    "sp_CalendarSpecialDay_ListAsEvents",
                    new { StartDate = start, EndDate = end, OnlyActive = true },
                    commandType: System.Data.CommandType.StoredProcedure)).ToList();
            }
            catch (SqlException ex) when (IsCompatibleWithFallback(ex))
            {
                var sql = @"
                    SELECT
                        CAST(Date AS DATETIME2)  AS Date,
                        N'SpecialDay'            AS Type,
                        Title                    AS Title,
                        ISNULL(Detail, N'')      AS Detail,
                        Id                       AS ReferenceId,
                        NULL                     AS Link,
                        1                        AS Priority
                    FROM CalendarSpecialDays
                    WHERE (IsActive = 1)
                      AND (@StartDate IS NULL OR Date >= @StartDate)
                      AND (@EndDate   IS NULL OR Date <= @EndDate)
                    ORDER BY Date, Title";
                specialDayEvents = (await conn.QueryAsync<CalendarEvent>(
                    sql, new { StartDate = start, EndDate = end })).ToList();
            }

            return mainEvents
                .Concat(specialDayEvents)
                .OrderBy(x => x.Date)
                .ThenBy(x => x.Priority)
                .ThenBy(x => x.Title)
                .ToList();
        });
    }

    /// <summary>
    /// Ozel gun tanimlarini listeler.
    /// </summary>
    public async Task<ServiceResult<List<CalendarSpecialDay>>> ListSpecialDaysAsync(DateTime? startDate = null, DateTime? endDate = null, bool onlyActive = true)
    {
        return await ExecuteServiceAsync<List<CalendarSpecialDay>>(async conn =>
        {
            try
            {
                return (await conn.QueryAsync<CalendarSpecialDay>(
                    "sp_CalendarSpecialDay_List",
                    new
                    {
                        StartDate = startDate?.Date,
                        EndDate = endDate?.Date,
                        OnlyActive = onlyActive
                    },
                    commandType: System.Data.CommandType.StoredProcedure)).ToList();
            }
            catch (SqlException ex) when (IsCompatibleWithFallback(ex))
            {
                var sql = @"
                    SELECT Id, Date, Title, TypeLookupId, Detail, IsAnnualRecurring, IsActive, CreatedAt
                    FROM CalendarSpecialDays
                    WHERE (@OnlyActive = 0 OR IsActive = 1)
                      AND (@StartDate IS NULL OR Date >= @StartDate)
                      AND (@EndDate IS NULL OR Date <= @EndDate)
                    ORDER BY Date, Title";
                return (await conn.QueryAsync<CalendarSpecialDay>(
                    sql, new { StartDate = startDate?.Date, EndDate = endDate?.Date, OnlyActive = onlyActive })).ToList();
            }
        });
    }

    /// <summary>
    /// Ozel gun kaydeder veya gunceller.
    /// </summary>
    public async Task<ServiceResult<int>> SaveSpecialDayAsync(CalendarSpecialDay model)
    {
        return await ExecuteServiceAsync<int>(async conn =>
        {
            try
            {
                return await conn.ExecuteScalarAsync<int>(
                    "sp_CalendarSpecialDay_Save",
                    new
                    {
                        model.Id,
                        Date = model.Date.Date,
                        model.Title,
                        TypeLookupId = model.TypeLookupId,
                        model.Detail,
                        IsAnnualRecurring = model.IsAnnualRecurring,
                        IsActive = model.IsActive
                    },
                    commandType: System.Data.CommandType.StoredProcedure);
            }
            catch (SqlException ex) when (IsCompatibleWithFallback(ex))
            {
                if (model.Id == 0)
                {
                    var sql = @"
                        INSERT INTO CalendarSpecialDays (Date, Title, TypeLookupId, Detail, IsAnnualRecurring, IsActive)
                        VALUES (@Date, @Title, @TypeLookupId, @Detail, @IsAnnualRecurring, @IsActive);
                        SELECT CAST(SCOPE_IDENTITY() AS INT)";
                    return await conn.ExecuteScalarAsync<int>(sql, new
                    {
                        Date = model.Date.Date,
                        model.Title,
                        TypeLookupId = model.TypeLookupId,
                        model.Detail,
                        IsAnnualRecurring = model.IsAnnualRecurring,
                        IsActive = model.IsActive
                    });
                }
                else
                {
                    var sql = @"
                        UPDATE CalendarSpecialDays
                        SET Date = @Date, Title = @Title, TypeLookupId = @TypeLookupId,
                            Detail = @Detail, IsAnnualRecurring = @IsAnnualRecurring, IsActive = @IsActive
                        WHERE Id = @Id;
                        SELECT @Id";
                    return await conn.ExecuteScalarAsync<int>(sql, new
                    {
                        model.Id,
                        Date = model.Date.Date,
                        model.Title,
                        TypeLookupId = model.TypeLookupId,
                        model.Detail,
                        IsAnnualRecurring = model.IsAnnualRecurring,
                        IsActive = model.IsActive
                    });
                }
            }
        });
    }

    /// <summary>
    /// Ozel gun kaydini siler.
    /// </summary>
    public async Task<ServiceResult> DeleteSpecialDayAsync(int id)
    {
        return await ExecuteServiceAsync(async conn =>
        {
            try
            {
                await conn.ExecuteAsync(
                    "sp_CalendarSpecialDay_Delete",
                    new { Id = id },
                    commandType: System.Data.CommandType.StoredProcedure);
            }
            catch (SqlException ex) when (IsCompatibleWithFallback(ex))
            {
                await conn.ExecuteAsync(
                    "DELETE FROM CalendarSpecialDays WHERE Id = @Id",
                    new { Id = id });
            }
        });
    }
}
