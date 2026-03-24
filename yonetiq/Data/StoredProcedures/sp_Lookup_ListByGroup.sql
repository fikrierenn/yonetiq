-- SP: sp_Lookup_ListByGroup
-- Kaynak: InfrastructureSeed.cs -> PrepareLookupsTableAsync
-- Tarih: 2026-03-23
-- Servis: LookupService
-- Aciklama: Belirtilen gruba ait aktif lookup degerlerini listeler.
IF OBJECT_ID('sp_Lookup_ListByGroup', 'P') IS NOT NULL
    DROP PROCEDURE sp_Lookup_ListByGroup;
GO

CREATE PROCEDURE sp_Lookup_ListByGroup
    @Group NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, [Group], Value, IsActive
    FROM Lookups
    WHERE [Group] = @Group AND IsActive = 1
    ORDER BY Value;
END
GO
