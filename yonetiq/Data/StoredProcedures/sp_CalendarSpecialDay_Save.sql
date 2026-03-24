-- SP: sp_CalendarSpecialDay_Save
-- Kaynak: InfrastructureSeed.cs -> PrepareCalendarSpecialDayInfrastructureAsync
-- Tarih: 2026-03-23
-- Servis: CalendarService
-- Aciklama: Ozel gun kaydeder (INSERT) veya gunceller (UPDATE). Id=0 ise yeni kayit olusturur.
IF OBJECT_ID('sp_CalendarSpecialDay_Save', 'P') IS NOT NULL
    DROP PROCEDURE sp_CalendarSpecialDay_Save;
GO

CREATE PROCEDURE sp_CalendarSpecialDay_Save
    @Id INT = 0,
    @Date DATE,
    @Title NVARCHAR(200),
    @TypeLookupId INT,
    @Detail NVARCHAR(1000) = NULL,
    @IsAnnualRecurring BIT = 0,
    @IsActive BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    IF @Id = 0
    BEGIN
        INSERT INTO CalendarSpecialDays (Date, Title, TypeLookupId, Detail, IsAnnualRecurring, IsActive)
        VALUES (@Date, @Title, @TypeLookupId, @Detail, @IsAnnualRecurring, @IsActive);
        SELECT CAST(SCOPE_IDENTITY() AS INT);
        RETURN;
    END
    UPDATE CalendarSpecialDays
    SET Date = @Date, Title = @Title, TypeLookupId = @TypeLookupId, Detail = @Detail, IsAnnualRecurring = @IsAnnualRecurring, IsActive = @IsActive
    WHERE Id = @Id;
    SELECT @Id;
END
GO
