-- ============================================================
-- Yonet Veritabani Semasi - schema_all.sql
-- Idempotent: Guvenle tekrar calistirilabilir.
-- ============================================================

-- ------------------------------------------------------------
-- 1) Toplantilar
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Toplantilar')
BEGIN
    CREATE TABLE dbo.Toplantilar
    (
        Id             INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Baslik         NVARCHAR(200) NOT NULL,
        Tarih          DATETIME2 NOT NULL,
        Yer            NVARCHAR(200) NOT NULL CONSTRAINT DF_Toplantilar_Yer DEFAULT N'',
        ToplantiLinki  NVARCHAR(500) NULL,
        Katilimcilar   NVARCHAR(500) NOT NULL CONSTRAINT DF_Toplantilar_Katilimcilar DEFAULT N'',
        Gundem         NVARCHAR(MAX) NOT NULL CONSTRAINT DF_Toplantilar_Gundem DEFAULT N'',
        Notlar         NVARCHAR(MAX) NOT NULL CONSTRAINT DF_Toplantilar_Notlar DEFAULT N'',
        OlusturmaTarih DATETIME2 NOT NULL CONSTRAINT DF_Toplantilar_OlusturmaTarih DEFAULT GETUTCDATE()
    );

    PRINT 'Tablo olusturuldu: Toplantilar';
END;
GO

IF COL_LENGTH('dbo.Toplantilar', 'Yer') IS NULL
BEGIN
    ALTER TABLE dbo.Toplantilar ADD Yer NVARCHAR(200) NOT NULL CONSTRAINT DF_Toplantilar_Yer2 DEFAULT N'';
    PRINT 'Kolon eklendi: Toplantilar.Yer';
END;
GO

IF COL_LENGTH('dbo.Toplantilar', 'ToplantiLinki') IS NULL
BEGIN
    ALTER TABLE dbo.Toplantilar ADD ToplantiLinki NVARCHAR(500) NULL;
    PRINT 'Kolon eklendi: Toplantilar.ToplantiLinki';
END;
GO

-- ------------------------------------------------------------
-- 2) Kullanicilar
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Kullanicilar')
BEGIN
    CREATE TABLE dbo.Kullanicilar
    (
        Id             INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        AdSoyad        NVARCHAR(150) NOT NULL,
        Eposta         NVARCHAR(200) NULL,
        Rol            NVARCHAR(50) NOT NULL CONSTRAINT DF_Kullanicilar_Rol DEFAULT N'Personel',
        Aktif          BIT NOT NULL CONSTRAINT DF_Kullanicilar_Aktif DEFAULT 1,
        OlusturmaTarih DATETIME2 NOT NULL CONSTRAINT DF_Kullanicilar_OlusturmaTarih DEFAULT GETUTCDATE()
    );

    CREATE UNIQUE INDEX UX_Kullanicilar_Eposta
        ON dbo.Kullanicilar(Eposta)
        WHERE Eposta IS NOT NULL;

    CREATE INDEX IX_Kullanicilar_Aktif_AdSoyad
        ON dbo.Kullanicilar(Aktif, AdSoyad);

    PRINT 'Tablo olusturuldu: Kullanicilar';
END;
GO

-- ------------------------------------------------------------
-- 3) Kararlar
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Kararlar')
BEGIN
    CREATE TABLE dbo.Kararlar
    (
        Id                 INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ToplantiId         INT NOT NULL,
        Icerik             NVARCHAR(MAX) NOT NULL,
        Sorumlu            NVARCHAR(100) NOT NULL CONSTRAINT DF_Kararlar_Sorumlu DEFAULT N'',
        SorumluKullaniciId INT NULL,
        BitisTarihi        DATETIME2 NULL,
        Durum              NVARCHAR(20) NOT NULL CONSTRAINT DF_Kararlar_Durum DEFAULT N'Bekliyor',
        GoreveDonustuMu    BIT NOT NULL CONSTRAINT DF_Kararlar_GoreveDonustuMu DEFAULT 0,
        BagliGorevId       INT NULL,

        CONSTRAINT CK_Kararlar_Durum CHECK (Durum IN (N'Bekliyor', N'Devam', N'Tamamlandi')),
        CONSTRAINT FK_Kararlar_Toplantilar FOREIGN KEY (ToplantiId) REFERENCES dbo.Toplantilar(Id) ON DELETE CASCADE,
        CONSTRAINT FK_Kararlar_Kullanicilar FOREIGN KEY (SorumluKullaniciId) REFERENCES dbo.Kullanicilar(Id) ON DELETE SET NULL
    );

    CREATE INDEX IX_Kararlar_ToplantiId ON dbo.Kararlar(ToplantiId);
    CREATE INDEX IX_Kararlar_Durum ON dbo.Kararlar(Durum);
    CREATE INDEX IX_Kararlar_SorumluKullaniciId ON dbo.Kararlar(SorumluKullaniciId);
    CREATE INDEX IX_Kararlar_BagliGorevId ON dbo.Kararlar(BagliGorevId);

    PRINT 'Tablo olusturuldu: Kararlar';
END;
GO

IF COL_LENGTH('dbo.Kararlar', 'SorumluKullaniciId') IS NULL
BEGIN
    ALTER TABLE dbo.Kararlar ADD SorumluKullaniciId INT NULL;
    PRINT 'Kolon eklendi: Kararlar.SorumluKullaniciId';
END;
GO

IF COL_LENGTH('dbo.Kararlar', 'GoreveDonustuMu') IS NULL
BEGIN
    ALTER TABLE dbo.Kararlar ADD GoreveDonustuMu BIT NOT NULL CONSTRAINT DF_Kararlar_GoreveDonustuMu2 DEFAULT 0;
    PRINT 'Kolon eklendi: Kararlar.GoreveDonustuMu';
END;
GO

IF COL_LENGTH('dbo.Kararlar', 'BagliGorevId') IS NULL
BEGIN
    ALTER TABLE dbo.Kararlar ADD BagliGorevId INT NULL;
    PRINT 'Kolon eklendi: Kararlar.BagliGorevId';
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Kararlar_Kullanicilar')
BEGIN
    ALTER TABLE dbo.Kararlar
        ADD CONSTRAINT FK_Kararlar_Kullanicilar
        FOREIGN KEY (SorumluKullaniciId) REFERENCES dbo.Kullanicilar(Id) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Kararlar_SorumluKullaniciId' AND object_id = OBJECT_ID('dbo.Kararlar'))
BEGIN
    CREATE INDEX IX_Kararlar_SorumluKullaniciId ON dbo.Kararlar(SorumluKullaniciId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Kararlar_BagliGorevId' AND object_id = OBJECT_ID('dbo.Kararlar'))
BEGIN
    CREATE INDEX IX_Kararlar_BagliGorevId ON dbo.Kararlar(BagliGorevId);
END;
GO

-- ------------------------------------------------------------
-- 4) Gorevler
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Gorevler')
BEGIN
    CREATE TABLE dbo.Gorevler
    (
        Id                INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Baslik            NVARCHAR(200) NOT NULL,
        Aciklama          NVARCHAR(MAX) NOT NULL CONSTRAINT DF_Gorevler_Aciklama DEFAULT N'',
        Atanan            NVARCHAR(100) NOT NULL,
        AtananKullaniciId INT NULL,
        AtanmaTarihi      DATE NOT NULL CONSTRAINT DF_Gorevler_AtanmaTarihi DEFAULT CAST(GETDATE() AS DATE),
        BitisTarihi       DATE NULL,
        Oncelik           NVARCHAR(20) NOT NULL CONSTRAINT DF_Gorevler_Oncelik DEFAULT N'Orta',
        Durum             NVARCHAR(20) NOT NULL CONSTRAINT DF_Gorevler_Durum DEFAULT N'Bekliyor',

        CONSTRAINT CK_Gorevler_Oncelik CHECK (Oncelik IN (N'Dusuk', N'Orta', N'Yuksek')),
        CONSTRAINT CK_Gorevler_Durum CHECK (Durum IN (N'Bekliyor', N'Devam', N'Tamamlandi')),
        CONSTRAINT FK_Gorevler_Kullanicilar FOREIGN KEY (AtananKullaniciId) REFERENCES dbo.Kullanicilar(Id) ON DELETE SET NULL
    );

    CREATE INDEX IX_Gorevler_Atanan ON dbo.Gorevler(Atanan);
    CREATE INDEX IX_Gorevler_AtananKullaniciId ON dbo.Gorevler(AtananKullaniciId);
    CREATE INDEX IX_Gorevler_Durum ON dbo.Gorevler(Durum);
    CREATE INDEX IX_Gorevler_Oncelik ON dbo.Gorevler(Oncelik);

    PRINT 'Tablo olusturuldu: Gorevler';
END;
GO

IF COL_LENGTH('dbo.Gorevler', 'AtananKullaniciId') IS NULL
BEGIN
    ALTER TABLE dbo.Gorevler ADD AtananKullaniciId INT NULL;
    PRINT 'Kolon eklendi: Gorevler.AtananKullaniciId';
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Gorevler_Kullanicilar')
BEGIN
    ALTER TABLE dbo.Gorevler
        ADD CONSTRAINT FK_Gorevler_Kullanicilar
        FOREIGN KEY (AtananKullaniciId) REFERENCES dbo.Kullanicilar(Id) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Gorevler_AtananKullaniciId' AND object_id = OBJECT_ID('dbo.Gorevler'))
BEGIN
    CREATE INDEX IX_Gorevler_AtananKullaniciId ON dbo.Gorevler(AtananKullaniciId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Kararlar_Gorevler')
BEGIN
    ALTER TABLE dbo.Kararlar
        ADD CONSTRAINT FK_Kararlar_Gorevler
        FOREIGN KEY (BagliGorevId) REFERENCES dbo.Gorevler(Id) ON DELETE SET NULL;
END;
GO

-- ------------------------------------------------------------
-- 5) SorguKayitlar
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SorguKayitlar')
BEGIN
    CREATE TABLE dbo.SorguKayitlar
    (
        Id               INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Ad               NVARCHAR(200) NOT NULL,
        Aciklama         NVARCHAR(500) NOT NULL CONSTRAINT DF_SorguKayitlar_Aciklama DEFAULT N'',
        Sql              NVARCHAR(MAX) NOT NULL,
        SonucTipi        NVARCHAR(30) NOT NULL CONSTRAINT DF_SorguKayitlar_SonucTipi DEFAULT N'Tablo',
        Renk             NVARCHAR(20) NOT NULL CONSTRAINT DF_SorguKayitlar_Renk DEFAULT N'#1976d2',
        PivotSatirKolon  NVARCHAR(100) NULL,
        PivotSutunKolon  NVARCHAR(100) NULL,
        PivotDegerKolon  NVARCHAR(100) NULL,
        GuncellenmeTarih DATETIME2 NOT NULL CONSTRAINT DF_SorguKayitlar_GuncellenmeTarih DEFAULT GETUTCDATE()
    );

    PRINT 'Tablo olusturuldu: SorguKayitlar';
END;
GO

-- ------------------------------------------------------------
-- 6) SorguSetleri
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SorguSetleri')
BEGIN
    CREATE TABLE dbo.SorguSetleri
    (
        Id             INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Ad             NVARCHAR(200) NOT NULL,
        Aciklama       NVARCHAR(500) NOT NULL CONSTRAINT DF_SorguSetleri_Aciklama DEFAULT N'',
        Tip            NVARCHAR(20) NOT NULL CONSTRAINT DF_SorguSetleri_Tip DEFAULT N'Rapor',
        Ikon           NVARCHAR(100) NOT NULL CONSTRAINT DF_SorguSetleri_Ikon DEFAULT N'Dashboard',
        NavMenuGoster  BIT NOT NULL CONSTRAINT DF_SorguSetleri_NavMenuGoster DEFAULT 1,
        NavMenuSira    INT NOT NULL CONSTRAINT DF_SorguSetleri_NavMenuSira DEFAULT 0,
        OlusturmaTarih DATETIME2 NOT NULL CONSTRAINT DF_SorguSetleri_OlusturmaTarih DEFAULT GETUTCDATE(),

        CONSTRAINT CK_SorguSetleri_Tip CHECK (Tip IN (N'Dashboard', N'KpiPaneli', N'Rapor', N'Ozel'))
    );

    PRINT 'Tablo olusturuldu: SorguSetleri';
END;
GO

-- ------------------------------------------------------------
-- 7) SetiSorgular (M:N)
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SetiSorgular')
BEGIN
    CREATE TABLE dbo.SetiSorgular
    (
        Id         INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SetiId     INT NOT NULL,
        SorguId    INT NOT NULL,
        Sira       INT NOT NULL CONSTRAINT DF_SetiSorgular_Sira DEFAULT 0,
        GenislikMd INT NOT NULL CONSTRAINT DF_SetiSorgular_GenislikMd DEFAULT 6,

        CONSTRAINT FK_SetiSorgular_Setler FOREIGN KEY (SetiId) REFERENCES dbo.SorguSetleri(Id) ON DELETE CASCADE,
        CONSTRAINT FK_SetiSorgular_Sorgular FOREIGN KEY (SorguId) REFERENCES dbo.SorguKayitlar(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_SetiSorgular_SetiId ON dbo.SetiSorgular(SetiId);
    CREATE INDEX IX_SetiSorgular_SorguId ON dbo.SetiSorgular(SorguId);

    PRINT 'Tablo olusturuldu: SetiSorgular';
END;
GO

PRINT '';
PRINT '=== Sema uygulamasi tamamlandi ===';
