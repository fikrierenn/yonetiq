-- SP: sp_CalendarSpecialDay_Delete
-- Kaynak: InfrastructureSeed.cs -> PrepareCalendarSpecialDayInfrastructureAsync
-- Tarih: 2026-03-23
-- Servis: CalendarService
-- Aciklama: Belirtilen ozel gunu siler.
IF OBJECT_ID('sp_CalendarSpecialDay_Delete', 'P') IS NOT NULL
    DROP PROCEDURE sp_CalendarSpecialDay_Delete;
GO

CREATE PROCEDURE sp_CalendarSpecialDay_Delete
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM CalendarSpecialDays WHERE Id = @Id;
END
GO
