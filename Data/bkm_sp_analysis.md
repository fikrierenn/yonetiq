# BKM Kitap — urnOzt SP Analizi (25.03.2026)

## 1. urnOzt_bosTum — Tablo Hazırlık

**Ne yapıyor:** TRUNCATE değil! Eksik ürün×mağaza kombinasyonlarını INSERT ediyor.
```sql
INSERT INTO urnOzt(oztStkID, oztMekan, oztEtkin, oztAlt, oztFrmID)
SELECT stkID, mekanID, mekanUrnDurum,
       CASE WHEN mekanTip=0 THEN @sirketAdet
            WHEN mekanTip=1 THEN @firmaAdet ELSE 0 END,
       stkFirma
FROM urn, posMagaza
WHERE urnTip=0 AND mekanOzetle=1
  AND NOT EXISTS (SELECT * FROM urnOzt WHERE oztMekan=mekanID AND stkID=oztStkID)
```

**Kritik bulgular:**
- Cartesian product: `urn × posMagaza` (mevcut olmayanları ekle)
- `urnTip=0` → sadece normal ürünler (set/paket değil)
- `mekanOzetle=1` → sadece özetlenecek mağazalar
- NOT EXISTS ile idempotent — tekrar çalıştırılabilir

---

## 2. urnOzt365_tum — Satış Hesaplama (SON 365 GÜN)

**Kaynak tablo: `irsAyr` + `irs` (irsHrk DEĞİL!)**

```sql
-- Önce tüm sayısal kolonları sıfırla
UPDATE urnOzt SET oztGecen28=0, oztSon28=0, oztSon365=0,
    oztSon30=0, oztSon60=0, oztSon180=0, oztSon7=0, oztSon14=0,
    oztGcnHfErtGun=0, oztSon21=0, oztGecen365=0, stok=0

-- Sonra irsAyr + irs'den hesapla
UPDATE urnOzt SET ... FROM (
    SELECT ehStkID, eMekan,
        SUM(-ehAdet) AS stYil,                                    -- yıllık toplam
        SUM(CASE WHEN eTarih >= @tarih-6  THEN -ehAdet END) AS stSon7,
        SUM(CASE WHEN eTarih >= @tarih-13 THEN -ehAdet END) AS stSon14,
        SUM(CASE WHEN eTarih >= @tarih-20 THEN -ehAdet END) AS stSon21,
        SUM(CASE WHEN eTarih >= @tarih-27 THEN -ehAdet END) AS stSon28,
        SUM(CASE WHEN eTarih >= @tarih-29 THEN -ehAdet END) AS stSon30,
        SUM(CASE WHEN eTarih >= @tarih-59 THEN -ehAdet END) AS stSon60,
        SUM(CASE WHEN eTarih >= @tarih-179 THEN -ehAdet END) AS stSon180,
        -- Geçen yılın aynı 28 günü
        SUM(CASE WHEN eTarih <= @tarih-365+27 THEN -ehAdet END) AS stGecenSon28,
        -- Geçen haftanın ertesi günü (benchmark)
        SUM(CASE WHEN eTarih = @tarih-5 THEN -ehAdet END) AS stGcnHfErtGun
    FROM irsAyr
    INNER JOIN irs ON eID = ehID
    WHERE eTarih > @tarih-365
      AND eTip IN (1, 3, 4, 5, 100, 101)  -- satış + iade tipleri
    GROUP BY ehStkID, eMekan
) t WHERE ehStkID = oztStkID AND oztMekan = eMekan
```

**KRİTİK BULGULAR:**
1. **Kaynak `irsAyr` + `irs`, irsHrk DEĞİL** — irsAyr irsaliye ayrıntı, irsHrk hareket
2. **`-ehAdet` kullanıyor** — satışta adet negatif, -1 ile çarparak pozitife çeviriyor
3. **eTip IN (1,3,4,5,100,101)** — satış + iade birlikte, işaret farkıyla
4. **stok=0 yapıyor** — stok bu SP'de hesaplanmıyor! Başka yerde dolduruluyor (muhtemelen urnOzt_tumMekansal)
5. **`@tarih = GETDATE()-1`** — dünün tarihi baz alınıyor

---

## 3. urnOzt_hizTek — Satış Hızı Hesaplama

```sql
UPDATE urnOzt SET
    hiz = satis / (1 + DATEDIFF(d,
        CASE WHEN ilkAlis > mekanAcilisTarih
             THEN ilkAlis ELSE mekanAcilisTarih END,
        GETDATE()) - gun0)
FROM (
    SELECT eMekan, -1*SUM(ehAdet) AS satis
    FROM irs_vw
    WHERE eTip IN (1, 4, 100) AND ehStkID = @stkID
    GROUP BY eMekan
) s
INNER JOIN posMagaza ON eMekan = mekanID
WHERE oztMekan = eMekan AND oztStkID = @stkID
  AND (1 + DATEDIFF(d, ...) - gun0) <> 0  -- sıfıra bölme koruması
```

**Formül:** `hiz = toplam_satis / (aktif_gün_sayisi - stoksuz_gün_sayisi)`

**Analiz:**
- `irs_vw` view kullanıyor (irsAyr+irs birleşimi)
- Sadece satış tipleri (1,4,100) — iade dahil değil
- `gun0` = ürünün stoksuz kaldığı gün sayısı (stoksuz günler hesaptan çıkarılıyor)
- `ilkAlis` vs `mekanAcilisTarih` — hangisi daha geçse onu baz al
- **Tek ürün bazlı** — toplu güncelleme başka SP'de

---

## 4. urnOzt_tumMekansal — Ana Orchestrator

```sql
DECLARE mCrs CURSOR FOR
    SELECT mekanID FROM posMagaza
    WHERE mekanDurum=0 AND mekanAcilisTarih < GETDATE()

-- Her aktif mağaza için:
EXEC urnOzt_mekan @mekanID, @perakende
```

**Analiz:**
- **CURSOR** kullanıyor — mağaza mağaza döngü
- `mekanDurum=0` → aktif mağazalar (0=aktif, 1=kapalı?)
- Her mağaza için `urnOzt_mekan` çağrıyor — asıl stok hesaplaması orada
- `@perakende` parametresi var — perakende/toptan ayrımı

---

## PERFORMANS DEĞERLENDİRMESİ

### Sorunlar:
1. **CURSOR anti-pattern** — urnOzt_tumMekansal her mağazayı tek tek dolaşıyor
2. **stok=0 sıfırlama** — önce tüm tablo sıfırlanıp tekrar hesaplanıyor (expensive)
3. **irsAyr + irs JOIN** — 66M × join (index durumuna bağlı)
4. **Tek ürün SP'leri** — urnOzt_hizTek tek ürün bazlı, toplu güncelleme cursor ile

### Optimizasyon Önerileri:
1. CURSOR yerine SET-based işlem (tüm mağazaları tek sorguda hesapla)
2. Incremental güncelleme (sadece değişen ürünleri güncelle, tümünü sıfırlama)
3. Indexed view veya materialized summary table
4. urnOzt_mekan SP'sinin içeriği kritik — onu da görmek lazım

---

## 5. urnOzt_fifoTek — FIFO Maliyet Hesaplama + Stok Devir Hızı

**Bu SP iki şey yapıyor:**
1. Çıkış satırlarına FIFO maliyet ataması (irsHrk.ehMlyt güncelleme)
2. Stok devir hızı hesaplama (urnOzt.devir)

**Kaynak: `irsHrk` tablosu (irsAyr değil!)**

```
FIFO Mantığı:
- gCrs (giriş cursor): ehAdetN > 0 olan satırlar (alış/giriş), tarih sıralı
- cCrs (çıkış cursor): ehAdetN < 0 olan satırlar (satış/çıkış), tarih sıralı

Her çıkış satırı için:
  1. En eski girişten birim maliyet al (@birimMaliyet = @girisMaliyet / @girisAdet)
  2. Çıkış adedi kadar girişten düş
  3. Giriş biterse sonraki girişe geç
  4. Hesaplanan FIFO maliyet → UPDATE irsHrk SET ehMlyt = @hesap WHERE CURRENT OF cCrs
  5. @fark += adet × DATEDIFF(giriş_tarihi, çıkış_tarihi) → devir günü
```

**Devir hızı formülü:**
```
devir = toplam_fark / toplam_cikis_adedi
     = SUM(adet × (çıkış_tarihi - giriş_tarihi)) / SUM(çıkış_adedi)
     = Ortalama stokta kalma süresi (gün)
```

**KRİTİK BULGULAR:**
1. **Çift CURSOR** — en büyük performans sorunu. Her ürün×mağaza için iki cursor açılıyor
2. **irsHrk.ehMlyt'yi doğrudan güncelliyor** — FIFO maliyet çıkış satırına yazılıyor
3. **Devir hızı = stokta kalma süresi (gün)** — düşük = iyi (hızlı satış)
4. `@birimMaliyet = @girisMaliyet / @girisAdet` → sıfıra bölme riski (korumasız!)
5. Tek ürün×mağaza bazlı — toplu çağrı cursor ile yapılıyor (urnOzt_tumMekansal)

---

## GENEL MİMARİ ÖZETİ

```
Gece 23:00 — DerinSis_Ozet Job:
  │
  ├── urnOzt_bosTum
  │   └── Eksik ürün×mağaza satırlarını INSERT (cartesian urn × posMagaza)
  │
  ├── urnOzt_tumMekansal (CURSOR: her aktif mağaza)
  │   └── urnOzt_mekan (?)
  │       └── Muhtemelen: stok bakiyesi hesapla (SUM(irsHrk.ehAdetN))
  │       └── FIFO maliyet (urnOzt_fifoTek → çift cursor)
  │       └── Satış hızı (urnOzt_hizTek)
  │       └── gun0 hesaplama (stoksuz gün sayısı)
  │
  ├── urnOzt_tumMekansalAcilisTarih
  │   └── ilkAlis, ilkSatis, sonSatis vb. tarih güncelleme
  │
  └── urnOzt365_tum
      └── irsAyr + irs'den son 7/14/21/28/30/60/180/365 gün satış
```

## İKİ TABLO — AYNI VERİ, FARKLI DETAY

**Doğrulandı:** `irsAyr` ve `irsHrk` birebir eşleşiyor (ehID + ehStkID ile JOIN).
Aynı başlık ID=7126722 için her ikisinde de 907 satır, aynı ürünler, aynı adetler.

| Tablo | Kolon | Amaç | Kullanıldığı Yer |
|-------|-------|------|-----------------|
| `irsAyr` (66M) | 31 kolon — tutar, indirim, KDV, birim, not | Detaylı satış raporu | urnOzt365_tum |
| `irsHrk` (57M) | 12 kolon — adet, tutar, maliyet, tip | Hafif/hızlı stok+maliyet | urnOzt_fifoTek |
| `irs_vw` | View (irsAyr+irs JOIN) | Kolay erişim | urnOzt_hizTek |

**Fark:** irsAyr'da ehIndirim, ehTutarKDV, ehKDV, ehi1-5 (indirim kademeleri) var. irsHrk'da yok.
**Neden iki tablo:** Performance — irsHrk 12 kolon ile stok/maliyet hesaplaması daha hızlı.

**YonetIQ kuralı:**
- Ciro/satış raporu → `irsAyr + irs` (detaylı, indirim/KDV dahil)
- Stok bakiye → `irsHrk` (hafif, SUM(ehAdetN))
- FIFO maliyet → `irsHrk.ehMlyt` (gece güncellenir)

---

## OPTİMİZASYON ÖNERİLERİ

### 1. FIFO Maliyet — Set-Based Yaklaşım
Çift cursor yerine window function ile:
```sql
-- Konsept (test gerektirir):
WITH girişler AS (
    SELECT ehstkID, ehMekan, ehAdetN, ehMlyt, ehTrhS,
           SUM(ehAdetN) OVER (PARTITION BY ehstkID, ehMekan ORDER BY ehTrhS) AS RunningTotal
    FROM irsHrk WHERE ehAdetN > 0
),
çıkışlar AS (
    SELECT ehstkID, ehMekan, -ehAdetN AS adet, ehTrhS,
           SUM(-ehAdetN) OVER (PARTITION BY ehstkID, ehMekan ORDER BY ehTrhS) AS RunningTotal
    FROM irsHrk WHERE ehAdetN < 0
)
-- Running total matching ile FIFO eşleştirme
```

### 2. Incremental Güncelleme
Tümünü sıfırlayıp tekrar hesaplamak yerine:
```sql
-- Sadece dünden bu yana değişen ürün×mağaza çiftlerini güncelle
UPDATE urnOzt SET ...
FROM (
    SELECT ehstkID, ehMekan FROM irsHrk
    WHERE hrkTarih >= DATEADD(day, -1, GETDATE())
    GROUP BY ehstkID, ehMekan
) changed
WHERE oztStkID = changed.ehstkID AND oztMekan = changed.ehMekan
```

### 3. YonetIQ İçin Doğru Tablo Seçimi
- **Ciro raporu:** `irsAyr + irs` kullan (ERP ile tutarlı, urnOzt365_tum bunu kullanıyor)
- **Stok bakiye:** `urnOzt.stok` (gece) VEYA `SUM(irsHrk.ehAdetN)` (anlık)
- **Satış hızı:** `urnOzt.hiz` (precalculated)
- **Devir:** `urnOzt.devir` (FIFO bazlı, stokta kalma süresi gün)
- **Maliyet:** `irsHrk.ehMlyt` (FIFO maliyet, gece güncellenir)
- **Son N gün satış:** `urnOzt.oztSon7/30/365` (precalculated)
