-- ============================================================
-- Yonet Veritabani Nesneleri (SP / FN / TVF / VIEW)
-- Bu dosya schema_all.sql sonrasinda calistirilir.
-- ============================================================

-- ------------------------------------------------------------
-- VIEW'ler
-- ------------------------------------------------------------
CREATE OR ALTER VIEW dbo.vw_KararDetay
AS
SELECT
    k.Id,
    k.ToplantiId,
    k.Icerik,
    COALESCE(ku.AdSoyad, k.Sorumlu) AS Sorumlu,
    k.SorumluKullaniciId,
    k.BitisTarihi,
    k.Durum,
    k.GoreveDonustuMu,
    k.BagliGorevId,
    t.Baslik AS ToplantiBaslik,
    t.Tarih AS ToplantiTarih
FROM dbo.Kararlar k
JOIN dbo.Toplantilar t ON t.Id = k.ToplantiId
LEFT JOIN dbo.Kullanicilar ku ON ku.Id = k.SorumluKullaniciId;
GO

CREATE OR ALTER VIEW dbo.vw_GorevOzet
AS
SELECT
    g.Id,
    g.Baslik,
    g.Aciklama,
    COALESCE(ku.AdSoyad, g.Atanan) AS Atanan,
    g.AtananKullaniciId,
    g.AtanmaTarihi,
    g.BitisTarihi,
    g.Oncelik,
    g.Durum,
    CASE g.Oncelik WHEN 'Yuksek' THEN 1 WHEN 'Orta' THEN 2 ELSE 3 END AS OncelikSira
FROM dbo.Gorevler g
LEFT JOIN dbo.Kullanicilar ku ON ku.Id = g.AtananKullaniciId;
GO

CREATE OR ALTER VIEW dbo.vw_TakvimOlaylari
AS
SELECT
    CAST(t.Tarih AS date) AS Tarih,
    N'Toplanti' AS Tur,
    t.Baslik AS Baslik,
    N'Saat: ' + FORMAT(t.Tarih, 'HH:mm') + N' | Yer: ' + ISNULL(NULLIF(t.Yer, N''), N'Belirtilmedi') AS Detay,
    t.Id AS ReferansId,
    N'/toplanti/' + CAST(t.Id AS nvarchar(20)) AS Link,
    1 AS Oncelik
FROM dbo.Toplantilar t
UNION ALL
SELECT
    CAST(k.BitisTarihi AS date) AS Tarih,
    N'Karar Son Tarih' AS Tur,
    k.Icerik AS Baslik,
    N'Sorumlu: ' + ISNULL(k.Sorumlu, N'') + N' - Durum: ' + k.Durum AS Detay,
    k.ToplantiId AS ReferansId,
    N'/toplanti/' + CAST(k.ToplantiId AS nvarchar(20)) AS Link,
    CASE WHEN k.Durum = 'Bekliyor' THEN 0 ELSE 2 END AS Oncelik
FROM dbo.vw_KararDetay k
WHERE k.BitisTarihi IS NOT NULL
UNION ALL
SELECT
    CAST(g.AtanmaTarihi AS date) AS Tarih,
    N'Gorev Atama' AS Tur,
    g.Baslik,
    N'Atanan: ' + g.Atanan AS Detay,
    g.Id AS ReferansId,
    N'/gorev/duzenle/' + CAST(g.Id AS nvarchar(20)) AS Link,
    2 AS Oncelik
FROM dbo.vw_GorevOzet g
UNION ALL
SELECT
    CAST(g.BitisTarihi AS date) AS Tarih,
    N'Gorev Son Tarih' AS Tur,
    g.Baslik,
    N'Oncelik: ' + g.Oncelik + N' - Durum: ' + g.Durum AS Detay,
    g.Id AS ReferansId,
    N'/gorev/duzenle/' + CAST(g.Id AS nvarchar(20)) AS Link,
    CASE WHEN g.Durum = 'Tamamlandi' THEN 3 ELSE 0 END AS Oncelik
FROM dbo.vw_GorevOzet g
WHERE g.BitisTarihi IS NOT NULL;
GO

CREATE OR ALTER VIEW dbo.vw_TabloSemasi
AS
SELECT
    c.TABLE_NAME,
    c.COLUMN_NAME,
    c.ORDINAL_POSITION
FROM INFORMATION_SCHEMA.COLUMNS c
JOIN INFORMATION_SCHEMA.TABLES t ON t.TABLE_NAME = c.TABLE_NAME
WHERE t.TABLE_TYPE = 'BASE TABLE'
  AND c.TABLE_NAME NOT LIKE '__EF%'
  AND c.TABLE_NAME <> 'sysdiagrams';
GO

-- ------------------------------------------------------------
-- Fonksiyonlar
-- ------------------------------------------------------------
CREATE OR ALTER FUNCTION dbo.fn_BekleyenKararSayisi()
RETURNS INT
AS
BEGIN
    DECLARE @sonuc INT;

    SELECT @sonuc = COUNT(*)
    FROM dbo.Kararlar
    WHERE Durum = 'Bekliyor';

    RETURN ISNULL(@sonuc, 0);
END;
GO

CREATE OR ALTER FUNCTION dbo.fn_BekleyenGorevSayisi(@Gun INT)
RETURNS INT
AS
BEGIN
    DECLARE @sonuc INT;

    SELECT @sonuc = COUNT(*)
    FROM dbo.Gorevler
    WHERE Durum <> 'Tamamlandi'
      AND BitisTarihi <= DATEADD(day, @Gun, CAST(GETDATE() AS date));

    RETURN ISNULL(@sonuc, 0);
END;
GO

CREATE OR ALTER FUNCTION dbo.tvf_SetSorgulari(@SetId INT)
RETURNS TABLE
AS
RETURN
(
    SELECT
        ss.Id AS SetSorguId,
        ss.SetiId AS SetId,
        ss.SorguId AS BagliSorguId,
        ss.Sira,
        ss.GenislikMd,
        sk.Id AS SorguKayitId,
        sk.Ad,
        sk.Aciklama,
        sk.Sql,
        sk.SonucTipi,
        sk.Renk,
        sk.PivotSatirKolon,
        sk.PivotSutunKolon,
        sk.PivotDegerKolon,
        sk.GuncellenmeTarih
    FROM dbo.SetiSorgular ss
    JOIN dbo.SorguKayitlar sk ON sk.Id = ss.SorguId
    WHERE ss.SetiId = @SetId
);
GO

-- ------------------------------------------------------------
-- Kullanici SP'leri
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_Kullanici_Listele
    @SadeceAktif BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, AdSoyad, Eposta, Rol, Aktif, OlusturmaTarih
    FROM dbo.Kullanicilar
    WHERE (@SadeceAktif = 0 OR Aktif = 1)
    ORDER BY Aktif DESC, AdSoyad;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Kullanici_Getir
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, AdSoyad, Eposta, Rol, Aktif, OlusturmaTarih
    FROM dbo.Kullanicilar
    WHERE Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Kullanici_Kaydet
    @Id INT,
    @AdSoyad NVARCHAR(150),
    @Eposta NVARCHAR(200) = NULL,
    @Rol NVARCHAR(50) = N'Personel',
    @Aktif BIT = 1,
    @OlusturmaTarih DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    IF ISNULL(@Id, 0) = 0
    BEGIN
        INSERT INTO dbo.Kullanicilar (AdSoyad, Eposta, Rol, Aktif, OlusturmaTarih)
        VALUES (@AdSoyad, @Eposta, @Rol, @Aktif, @OlusturmaTarih);

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
        RETURN;
    END

    UPDATE dbo.Kullanicilar
    SET AdSoyad = @AdSoyad,
        Eposta = @Eposta,
        Rol = @Rol,
        Aktif = @Aktif
    WHERE Id = @Id;

    SELECT @Id AS Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Kullanici_Sil
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Kullanicilar
    SET Aktif = 0
    WHERE Id = @Id;
END;
GO

-- ------------------------------------------------------------
-- Toplanti ve Karar SP'leri
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_Toplanti_Listele
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM dbo.Toplantilar
    ORDER BY Tarih DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Toplanti_Getir
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM dbo.Toplantilar
    WHERE Id = @Id;

    SELECT Id, ToplantiId, Icerik, Sorumlu, SorumluKullaniciId, BitisTarihi, Durum, GoreveDonustuMu, BagliGorevId
    FROM dbo.vw_KararDetay
    WHERE ToplantiId = @Id
    ORDER BY Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Toplanti_Kaydet
    @Id INT,
    @Baslik NVARCHAR(200),
    @Tarih DATETIME2,
    @Yer NVARCHAR(200),
    @ToplantiLinki NVARCHAR(500) = NULL,
    @Katilimcilar NVARCHAR(500),
    @Gundem NVARCHAR(MAX),
    @Notlar NVARCHAR(MAX),
    @OlusturmaTarih DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    IF ISNULL(@Id, 0) = 0
    BEGIN
        INSERT INTO dbo.Toplantilar (Baslik, Tarih, Yer, ToplantiLinki, Katilimcilar, Gundem, Notlar, OlusturmaTarih)
        VALUES (@Baslik, @Tarih, @Yer, @ToplantiLinki, @Katilimcilar, @Gundem, @Notlar, @OlusturmaTarih);

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
        RETURN;
    END

    UPDATE dbo.Toplantilar
    SET Baslik = @Baslik,
        Tarih = @Tarih,
        Yer = @Yer,
        ToplantiLinki = @ToplantiLinki,
        Katilimcilar = @Katilimcilar,
        Gundem = @Gundem,
        Notlar = @Notlar
    WHERE Id = @Id;

    SELECT @Id AS Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Toplanti_Sil
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.Toplantilar
    WHERE Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Karar_Kaydet
    @Id INT,
    @ToplantiId INT,
    @Icerik NVARCHAR(MAX),
    @Sorumlu NVARCHAR(100),
    @SorumluKullaniciId INT = NULL,
    @BitisTarihi DATETIME2 = NULL,
    @Durum NVARCHAR(20),
    @GoreveDonustuMu BIT = 0,
    @BagliGorevId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SorumluAd NVARCHAR(100) = @Sorumlu;

    IF (@SorumluKullaniciId IS NOT NULL AND (NULLIF(LTRIM(RTRIM(ISNULL(@SorumluAd, N''))), N'') IS NULL))
    BEGIN
        SELECT @SorumluAd = AdSoyad
        FROM dbo.Kullanicilar
        WHERE Id = @SorumluKullaniciId;
    END

    IF ISNULL(@Id, 0) = 0
    BEGIN
        INSERT INTO dbo.Kararlar (ToplantiId, Icerik, Sorumlu, SorumluKullaniciId, BitisTarihi, Durum, GoreveDonustuMu, BagliGorevId)
        VALUES (@ToplantiId, @Icerik, ISNULL(@SorumluAd, N''), @SorumluKullaniciId, @BitisTarihi, @Durum, @GoreveDonustuMu, @BagliGorevId);

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
        RETURN;
    END

    UPDATE dbo.Kararlar
    SET Icerik = @Icerik,
        Sorumlu = ISNULL(@SorumluAd, N''),
        SorumluKullaniciId = @SorumluKullaniciId,
        BitisTarihi = @BitisTarihi,
        Durum = @Durum,
        GoreveDonustuMu = @GoreveDonustuMu,
        BagliGorevId = @BagliGorevId
    WHERE Id = @Id;

    SELECT @Id AS Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Karar_DurumGuncelle
    @Id INT,
    @Durum NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Kararlar
    SET Durum = @Durum
    WHERE Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Karar_Sil
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.Kararlar
    WHERE Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Karar_GoreveDonustur
    @KararId INT,
    @Baslik NVARCHAR(200) = NULL,
    @Oncelik NVARCHAR(20) = N'Orta'
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Icerik NVARCHAR(MAX);
    DECLARE @Sorumlu NVARCHAR(100);
    DECLARE @SorumluKullaniciId INT;
    DECLARE @BitisTarihi DATETIME2;
    DECLARE @BagliGorevId INT;

    SELECT
        @Icerik = Icerik,
        @Sorumlu = Sorumlu,
        @SorumluKullaniciId = SorumluKullaniciId,
        @BitisTarihi = BitisTarihi,
        @BagliGorevId = BagliGorevId
    FROM dbo.Kararlar
    WHERE Id = @KararId;

    IF @Icerik IS NULL
    BEGIN
        RAISERROR(N'Karar kaydi bulunamadi.', 16, 1);
        RETURN;
    END

    IF @BagliGorevId IS NOT NULL
    BEGIN
        SELECT @BagliGorevId AS GorevId;
        RETURN;
    END

    DECLARE @GorevBaslik NVARCHAR(200) = NULLIF(LTRIM(RTRIM(ISNULL(@Baslik, N''))), N'');
    IF @GorevBaslik IS NULL
    BEGIN
        SET @GorevBaslik = LEFT(@Icerik, 200);
    END

    INSERT INTO dbo.Gorevler (Baslik, Aciklama, Atanan, AtananKullaniciId, AtanmaTarihi, BitisTarihi, Oncelik, Durum)
    VALUES
    (
        @GorevBaslik,
        @Icerik,
        ISNULL(@Sorumlu, N''),
        @SorumluKullaniciId,
        CAST(GETDATE() AS date),
        CAST(@BitisTarihi AS date),
        @Oncelik,
        N'Bekliyor'
    );

    SET @BagliGorevId = CAST(SCOPE_IDENTITY() AS INT);

    UPDATE dbo.Kararlar
    SET GoreveDonustuMu = 1,
        BagliGorevId = @BagliGorevId
    WHERE Id = @KararId;

    SELECT @BagliGorevId AS GorevId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Karar_SonKayitlar
    @Adet INT = 5
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP(@Adet)
        Id,
        ToplantiId,
        Icerik,
        Sorumlu,
        SorumluKullaniciId,
        BitisTarihi,
        Durum,
        GoreveDonustuMu,
        BagliGorevId,
        ToplantiBaslik,
        ToplantiTarih
    FROM dbo.vw_KararDetay
    ORDER BY ToplantiTarih DESC, Id DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Karar_BekleyenSayisi
AS
BEGIN
    SET NOCOUNT ON;

    SELECT dbo.fn_BekleyenKararSayisi() AS Deger;
END;
GO

-- ------------------------------------------------------------
-- Gorev SP'leri
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_Gorev_Listele
    @Atanan NVARCHAR(100) = NULL,
    @AtananKullaniciId INT = NULL,
    @Durum NVARCHAR(20) = NULL,
    @Baslangic DATE = NULL,
    @Bitis DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM dbo.vw_GorevOzet
    WHERE (@Atanan IS NULL OR Atanan = @Atanan)
      AND (@AtananKullaniciId IS NULL OR AtananKullaniciId = @AtananKullaniciId)
      AND (@Durum IS NULL OR Durum = @Durum)
      AND (@Baslangic IS NULL OR BitisTarihi IS NULL OR BitisTarihi >= @Baslangic)
      AND (@Bitis IS NULL OR BitisTarihi IS NULL OR BitisTarihi <= @Bitis)
    ORDER BY OncelikSira, BitisTarihi;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Gorev_Getir
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM dbo.vw_GorevOzet
    WHERE Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Gorev_Kaydet
    @Id INT,
    @Baslik NVARCHAR(200),
    @Aciklama NVARCHAR(MAX),
    @Atanan NVARCHAR(100),
    @AtananKullaniciId INT = NULL,
    @AtanmaTarihi DATE,
    @BitisTarihi DATE = NULL,
    @Oncelik NVARCHAR(20),
    @Durum NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @AtananAd NVARCHAR(100) = @Atanan;

    IF (@AtananKullaniciId IS NOT NULL AND (NULLIF(LTRIM(RTRIM(ISNULL(@AtananAd, N''))), N'') IS NULL))
    BEGIN
        SELECT @AtananAd = AdSoyad
        FROM dbo.Kullanicilar
        WHERE Id = @AtananKullaniciId;
    END

    IF ISNULL(@Id, 0) = 0
    BEGIN
        INSERT INTO dbo.Gorevler (Baslik, Aciklama, Atanan, AtananKullaniciId, AtanmaTarihi, BitisTarihi, Oncelik, Durum)
        VALUES (@Baslik, @Aciklama, ISNULL(@AtananAd, N''), @AtananKullaniciId, @AtanmaTarihi, @BitisTarihi, @Oncelik, @Durum);

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
        RETURN;
    END

    UPDATE dbo.Gorevler
    SET Baslik = @Baslik,
        Aciklama = @Aciklama,
        Atanan = ISNULL(@AtananAd, N''),
        AtananKullaniciId = @AtananKullaniciId,
        AtanmaTarihi = @AtanmaTarihi,
        BitisTarihi = @BitisTarihi,
        Oncelik = @Oncelik,
        Durum = @Durum
    WHERE Id = @Id;

    SELECT @Id AS Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Gorev_DurumGuncelle
    @Id INT,
    @Durum NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Gorevler
    SET Durum = @Durum
    WHERE Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Gorev_Sil
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.Gorevler
    WHERE Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Gorev_BekleyenListele
    @Adet INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP(@Adet) *
    FROM dbo.vw_GorevOzet
    WHERE Durum <> 'Tamamlandi'
    ORDER BY OncelikSira, BitisTarihi ASC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Gorev_AtananlariGetir
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Atanan
    FROM
    (
        SELECT DISTINCT AdSoyad AS Atanan FROM dbo.Kullanicilar WHERE Aktif = 1
        UNION
        SELECT DISTINCT Atanan FROM dbo.Gorevler WHERE NULLIF(LTRIM(RTRIM(Atanan)), N'') IS NOT NULL
    ) AS kaynak
    ORDER BY Atanan;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Gorev_BekleyenSayisi
    @Gun INT = 7
AS
BEGIN
    SET NOCOUNT ON;

    SELECT dbo.fn_BekleyenGorevSayisi(@Gun) AS Deger;
END;
GO

-- ------------------------------------------------------------
-- Takvim ve sema SP'leri
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_Takvim_Listele
    @Baslangic DATE = NULL,
    @Bitis DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Tarih,
        Tur,
        Baslik,
        Detay,
        ReferansId,
        Link,
        Oncelik
    FROM dbo.vw_TakvimOlaylari
    WHERE (@Baslangic IS NULL OR Tarih >= @Baslangic)
      AND (@Bitis IS NULL OR Tarih <= @Bitis)
    ORDER BY Tarih, Oncelik, Baslik;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_TabloSemasi_Listele
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        TABLE_NAME,
        COLUMN_NAME,
        ORDINAL_POSITION
    FROM dbo.vw_TabloSemasi
    ORDER BY TABLE_NAME, ORDINAL_POSITION;
END;
GO

-- ------------------------------------------------------------
-- SorguKayit SP'leri
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_Sorgu_Listele
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM dbo.SorguKayitlar
    ORDER BY Ad;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Sorgu_Getir
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM dbo.SorguKayitlar
    WHERE Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Sorgu_Kaydet
    @Id INT,
    @Ad NVARCHAR(200),
    @Aciklama NVARCHAR(500),
    @Sql NVARCHAR(MAX),
    @SonucTipi NVARCHAR(30),
    @Renk NVARCHAR(20),
    @PivotSatirKolon NVARCHAR(100) = NULL,
    @PivotSutunKolon NVARCHAR(100) = NULL,
    @PivotDegerKolon NVARCHAR(100) = NULL,
    @GuncellenmeTarih DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    IF ISNULL(@Id, 0) = 0
    BEGIN
        INSERT INTO dbo.SorguKayitlar
            (Ad, Aciklama, Sql, SonucTipi, Renk, PivotSatirKolon, PivotSutunKolon, PivotDegerKolon, GuncellenmeTarih)
        VALUES
            (@Ad, @Aciklama, @Sql, @SonucTipi, @Renk, @PivotSatirKolon, @PivotSutunKolon, @PivotDegerKolon, @GuncellenmeTarih);

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
        RETURN;
    END

    UPDATE dbo.SorguKayitlar
    SET Ad = @Ad,
        Aciklama = @Aciklama,
        Sql = @Sql,
        SonucTipi = @SonucTipi,
        Renk = @Renk,
        PivotSatirKolon = @PivotSatirKolon,
        PivotSutunKolon = @PivotSutunKolon,
        PivotDegerKolon = @PivotDegerKolon,
        GuncellenmeTarih = @GuncellenmeTarih
    WHERE Id = @Id;

    SELECT @Id AS Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Sorgu_Sil
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.SorguKayitlar
    WHERE Id = @Id;
END;
GO

-- ------------------------------------------------------------
-- Set ve SetSorgu SP'leri
-- ------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_Set_Listele
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM dbo.SorguSetleri
    ORDER BY Tip, NavMenuSira, Ad;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Set_Getir
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM dbo.SorguSetleri
    WHERE Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Set_MenuListele
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM dbo.SorguSetleri
    WHERE NavMenuGoster = 1
    ORDER BY NavMenuSira, Ad;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Set_Kaydet
    @Id INT,
    @Ad NVARCHAR(200),
    @Aciklama NVARCHAR(500),
    @Tip NVARCHAR(20),
    @Ikon NVARCHAR(100),
    @NavMenuGoster BIT,
    @NavMenuSira INT,
    @OlusturmaTarih DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    IF ISNULL(@Id, 0) = 0
    BEGIN
        INSERT INTO dbo.SorguSetleri (Ad, Aciklama, Tip, Ikon, NavMenuGoster, NavMenuSira, OlusturmaTarih)
        VALUES (@Ad, @Aciklama, @Tip, @Ikon, @NavMenuGoster, @NavMenuSira, @OlusturmaTarih);

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
        RETURN;
    END

    UPDATE dbo.SorguSetleri
    SET Ad = @Ad,
        Aciklama = @Aciklama,
        Tip = @Tip,
        Ikon = @Ikon,
        NavMenuGoster = @NavMenuGoster,
        NavMenuSira = @NavMenuSira
    WHERE Id = @Id;

    SELECT @Id AS Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Set_Sil
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.SorguSetleri
    WHERE Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SetSorgu_SetIcineGoreListele
    @SetId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        SetSorguId,
        SetId,
        BagliSorguId,
        Sira,
        GenislikMd,
        SorguKayitId,
        Ad,
        Aciklama,
        Sql,
        SonucTipi,
        Renk,
        PivotSatirKolon,
        PivotSutunKolon,
        PivotDegerKolon,
        GuncellenmeTarih
    FROM dbo.tvf_SetSorgulari(@SetId)
    ORDER BY Sira;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SetSorgu_Ekle
    @SetId INT,
    @SorguId INT,
    @GenislikMd INT = 6,
    @Sira INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @HedefSira INT = ISNULL(@Sira, ISNULL((SELECT MAX(Sira) FROM dbo.SetiSorgular WHERE SetiId = @SetId), -1) + 1);

    INSERT INTO dbo.SetiSorgular (SetiId, SorguId, Sira, GenislikMd)
    VALUES (@SetId, @SorguId, @HedefSira, @GenislikMd);
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SetSorgu_Sil
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.SetiSorgular
    WHERE Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SetSorgu_GenislikGuncelle
    @Id INT,
    @GenislikMd INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.SetiSorgular
    SET GenislikMd = @GenislikMd
    WHERE Id = @Id;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SetSorgu_SiraGuncelle
    @Id INT,
    @YeniSira INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.SetiSorgular
    SET Sira = @YeniSira
    WHERE Id = @Id;
END;
GO

PRINT 'db_objects.sql tamamlandi.';









