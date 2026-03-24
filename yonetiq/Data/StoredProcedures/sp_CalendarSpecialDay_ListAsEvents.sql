-- SP: sp_CalendarSpecialDay_ListAsEvents
-- Kaynak: InfrastructureSeed.cs -> PrepareCalendarSpecialDayInfrastructureAsync
-- Tarih: 2026-03-23
-- Servis: CalendarService
-- Aciklama: Ozel gunleri takvim event formati olarak dondurur (Date, Type, Title, Detail, ReferenceId, Link, Priority).
IF OBJECT_ID('sp_CalendarSpecialDay_ListAsEvents', 'P') IS NOT NULL
    DROP PROCEDURE sp_CalendarSpecialDay_ListAsEvents;
GO

CREATE PROCEDURE sp_CalendarSpecialDay_ListAsEvents
    @StartDate DATE = NULL,
    @EndDate   DATE = NULL,
    @OnlyActive BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        CAST(Date AS DATETIME2)  AS Date,
        N'SpecialDay'            AS Type,
        Title                    AS Title,
        ISNULL(Detail, N'')      AS Detail,
        Id                       AS ReferenceId,
        NULL                     AS Link,
        1                        AS Priority
    FROM CalendarSpecialDays
    WHERE (@OnlyActive = 0 OR IsActive = 1)
      AND (@StartDate IS NULL OR Date >= @StartDate)
      AND (@EndDate   IS NULL OR Date <= @EndDate)
    ORDER BY Date, Title;
END
GO
