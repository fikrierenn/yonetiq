-- SP: sp_CalendarSpecialDay_List
-- Kaynak: InfrastructureSeed.cs -> PrepareCalendarSpecialDayInfrastructureAsync
-- Tarih: 2026-03-23
-- Servis: CalendarService
-- Aciklama: Ozel gunleri tarih araligina ve aktiflik durumuna gore listeler.
IF OBJECT_ID('sp_CalendarSpecialDay_List', 'P') IS NOT NULL
    DROP PROCEDURE sp_CalendarSpecialDay_List;
GO

CREATE PROCEDURE sp_CalendarSpecialDay_List
    @StartDate DATE = NULL,
    @EndDate DATE = NULL,
    @OnlyActive BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Date, Title, TypeLookupId, Detail, IsAnnualRecurring, IsActive, CreatedAt
    FROM CalendarSpecialDays
    WHERE (@OnlyActive = 0 OR IsActive = 1)
      AND (@StartDate IS NULL OR Date >= @StartDate)
      AND (@EndDate IS NULL OR Date <= @EndDate)
    ORDER BY Date, Title;
END
GO
