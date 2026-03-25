-- ============================================================
-- Yonet baslangic verileri - seed.sql
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM dbo.Kullanicilar)
BEGIN
    INSERT INTO dbo.Kullanicilar (AdSoyad, Eposta, Rol, Aktif)
    VALUES
        (N'Genel Mudur', N'gm@yonet.local', N'Genel Mudur', 1),
        (N'Finans Direktoru', N'finans@yonet.local', N'Direktor', 1),
        (N'Satis Direktoru', N'satis@yonet.local', N'Direktor', 1),
        (N'IT Muduru', N'it@yonet.local', N'Mudur', 1),
        (N'Operasyon Muduru', N'operasyon@yonet.local', N'Mudur', 1);

    PRINT 'Seed: Kullanicilar eklendi';
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Toplantilar)
BEGIN
    DECLARE @t1 INT, @t2 INT;
    DECLARE @itId INT = (SELECT TOP(1) Id FROM dbo.Kullanicilar WHERE AdSoyad = N'IT Muduru');
    DECLARE @satisId INT = (SELECT TOP(1) Id FROM dbo.Kullanicilar WHERE AdSoyad = N'Satis Direktoru');
    DECLARE @operasyonId INT = (SELECT TOP(1) Id FROM dbo.Kullanicilar WHERE AdSoyad = N'Operasyon Muduru');

    INSERT INTO dbo.Toplantilar (Baslik, Tarih, Yer, ToplantiLinki, Katilimcilar, Gundem, Notlar)
    VALUES
    (
        N'Q1 2026 Strateji Degerlendirmesi',
        '2026-01-15 09:30:00',
        N'Genel Mudurluk Toplanti Salonu A',
        N'https://meet.google.com/new',
        N'Genel Mudur, Finans Direktoru, Satis Direktoru, IT Muduru',
        N'1. 2026 hedefleri\n2. Departman performanslari\n3. Butce revizyonu',
        N'Departman raporlari degerlendirildi.'
    );
    SET @t1 = SCOPE_IDENTITY();

    INSERT INTO dbo.Kararlar (ToplantiId, Icerik, Sorumlu, SorumluKullaniciId, BitisTarihi, Durum, GoreveDonustuMu, BagliGorevId)
    VALUES
        (@t1, N'IT altyapi yatirimi Q2 basinda baslatilacak.', N'IT Muduru', @itId, '2026-03-31', 'Devam', 0, NULL),
        (@t1, N'Satis hedefi %15 artirildi.', N'Satis Direktoru', @satisId, '2026-12-31', 'Bekliyor', 0, NULL);

    INSERT INTO dbo.Toplantilar (Baslik, Tarih, Yer, ToplantiLinki, Katilimcilar, Gundem, Notlar)
    VALUES
    (
        N'Operasyonel Verimlilik Toplantisi',
        '2026-02-10 14:00:00',
        N'Operasyon Binasi Konferans Salonu',
        N'https://meet.google.com/new',
        N'Genel Mudur, Operasyon Muduru, IK Direktoru',
        N'1. Surec iyilestirme\n2. Calisan memnuniyeti',
        N'Q2 icin aksiyon plani olusturuldu.'
    );
    SET @t2 = SCOPE_IDENTITY();

    INSERT INTO dbo.Kararlar (ToplantiId, Icerik, Sorumlu, SorumluKullaniciId, BitisTarihi, Durum, GoreveDonustuMu, BagliGorevId)
    VALUES
        (@t2, N'Uretim hatasi orani %2 altina cekilecek.', N'Operasyon Muduru', @operasyonId, '2026-06-30', 'Bekliyor', 0, NULL);

    PRINT 'Seed: Toplantilar ve Kararlar eklendi';
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Gorevler)
BEGIN
    DECLARE @finansId INT = (SELECT TOP(1) Id FROM dbo.Kullanicilar WHERE AdSoyad = N'Finans Direktoru');
    DECLARE @itId2 INT = (SELECT TOP(1) Id FROM dbo.Kullanicilar WHERE AdSoyad = N'IT Muduru');
    DECLARE @satisId2 INT = (SELECT TOP(1) Id FROM dbo.Kullanicilar WHERE AdSoyad = N'Satis Direktoru');

    INSERT INTO dbo.Gorevler (Baslik, Aciklama, Atanan, AtananKullaniciId, AtanmaTarihi, BitisTarihi, Oncelik, Durum)
    VALUES
        (N'Q1 Finans Raporu', N'Ocak-Mart gelir gider konsolidasyonu.', N'Finans Direktoru', @finansId, '2026-03-01', '2026-04-05', 'Yuksek', 'Devam'),
        (N'Lisans Yenileme', N'Yazilim lisans tekliflerini degerlendir.', N'IT Muduru', @itId2, '2026-03-05', '2026-03-20', 'Orta', 'Bekliyor'),
        (N'Aylik Satis Sunumu', N'Subat satis rakamlarini hazirla.', N'Satis Direktoru', @satisId2, '2026-03-08', '2026-03-12', 'Yuksek', 'Tamamlandi'),
        (N'Sunucu Bakim Penceresi', N'Mart sonu bakim etki analizini tamamla.', N'IT Muduru', @itId2, '2026-03-10', '2026-03-25', 'Dusuk', 'Bekliyor');

    PRINT 'Seed: Gorevler eklendi';
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SorguKayitlar)
BEGIN
    INSERT INTO dbo.SorguKayitlar (Ad, Aciklama, Sql, SonucTipi, Renk)
    VALUES
    (
        N'Bekleyen Kararlar',
        N'Bekleyen karar sayisi',
        N'SELECT COUNT(*) AS [Bekleyen Kararlar] FROM Kararlar WHERE Durum = ''Bekliyor''',
        'KpiKarti',
        '#d32f2f'
    ),
    (
        N'Kisi Bazli Gorev Tamamlanma',
        N'Kisi gorev tamamlama orani',
        N'SELECT Atanan AS [Kisi], CAST(100.0 * SUM(CASE WHEN Durum=''Tamamlandi'' THEN 1 ELSE 0 END) / COUNT(*) AS DECIMAL(5,1)) AS [Oran %] FROM Gorevler GROUP BY Atanan',
        'CokluKpi',
        '#388e3c'
    ),
    (
        N'Gorev Durum Dagilimi',
        N'Donut chart veri seti',
        N'SELECT Durum AS [Durum], COUNT(*) AS [Adet] FROM Gorevler GROUP BY Durum',
        'DonutChart',
        '#1976d2'
    ),
    (
        N'Aylik Toplanti Sayisi',
        N'Bar chart veri seti',
        N'SELECT FORMAT(Tarih, ''yyyy-MM'') AS [Ay], COUNT(*) AS [Toplanti] FROM Toplantilar GROUP BY FORMAT(Tarih, ''yyyy-MM'') ORDER BY [Ay]',
        'BarChart',
        '#1976d2'
    );

    PRINT 'Seed: SorguKayitlar eklendi';
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SorguSetleri)
BEGIN
    DECLARE @dashboardId INT;
    DECLARE @gorevId INT;

    INSERT INTO dbo.SorguSetleri (Ad, Aciklama, Tip, Ikon, NavMenuGoster, NavMenuSira)
    VALUES (N'Ana Dashboard', N'Kritik KPI ozet paneli', 'Dashboard', 'Dashboard', 1, 0);
    SET @dashboardId = SCOPE_IDENTITY();

    INSERT INTO dbo.SorguSetleri (Ad, Aciklama, Tip, Ikon, NavMenuGoster, NavMenuSira)
    VALUES (N'Gorev Durumu', N'Gorev KPI paneli', 'KpiPaneli', 'TaskAlt', 1, 1);
    SET @gorevId = SCOPE_IDENTITY();

    INSERT INTO dbo.SetiSorgular (SetiId, SorguId, Sira, GenislikMd)
    SELECT @dashboardId, Id, ROW_NUMBER() OVER (ORDER BY Id) - 1, 6
    FROM dbo.SorguKayitlar
    WHERE Ad IN (N'Bekleyen Kararlar', N'Gorev Durum Dagilimi', N'Aylik Toplanti Sayisi');

    INSERT INTO dbo.SetiSorgular (SetiId, SorguId, Sira, GenislikMd)
    SELECT @gorevId, Id, 0, 12
    FROM dbo.SorguKayitlar
    WHERE Ad = N'Kisi Bazli Gorev Tamamlanma';

    PRINT 'Seed: SorguSetleri ve SetiSorgular eklendi';
END;
GO

PRINT '';
PRINT '=== Seed tamamlandi ===';

