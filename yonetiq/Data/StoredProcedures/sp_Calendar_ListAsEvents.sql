-- SP: sp_Calendar_ListAsEvents
-- Kaynak: InfrastructureSeed.cs -> PrepareCalendarSpecialDayInfrastructureAsync
-- Tarih: 2026-03-23
-- Servis: CalendarService
-- Aciklama: Toplanti ve gorevleri birlestirerek takvim event listesi olusturur (UNION ALL).
IF OBJECT_ID('sp_Calendar_ListAsEvents', 'P') IS NOT NULL
    DROP PROCEDURE sp_Calendar_ListAsEvents;
GO

CREATE PROCEDURE sp_Calendar_ListAsEvents
    @StartDate DATE = NULL,
    @EndDate   DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    -- Toplantilar
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

    -- DueDate'i olan gorevler
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

    ORDER BY Date, Priority, Title;
END
GO
