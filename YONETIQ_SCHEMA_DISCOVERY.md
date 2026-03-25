# YonetIQ — Schema Discovery & Onboarding Protokolü

**Tarih:** 25.03.2026 | **Amaç:** Claude Code, BKM Kitap'ın gerçek DB şemasını öğrenmeden kod yazmaz.

---

## BÖLÜM 1 — SCHEMA DISCOVERY SERVİSİ

Bu servis Claude Code'un her session başında çalıştıracağı keşif mantığı.

**Dosya:** `yonetiq/Data/Services/SchemaDiscoveryService.cs`

### Temel Akış:
1. `DiscoverAsync(dataSourceId)` → Tablo listesi, kolon detayları, satır sayıları, örnek değerler
2. `GenerateQuestions(discovery)` → Cevapsız soruları tespit et (satış tablosu, tutar kolonu, tarih, mağaza, kategori, iade, indirim)
3. `SaveMappingAsync(discovery, answers, outputPath)` → `schema_mapping.json`'a kaydet

### Modeller:
- `SchemaDiscoveryResult`: tablolar, satış/ürün/mağaza adayları
- `TableInfo`: tablo adı, satır sayısı, kolonlar, örnek değerler
- `ColumnInfo`: kolon adı, tip, nullable, PK/FK
- `SchemaQuestion`: id, priority (Critical/High/Medium/Low), soru, seçenekler
- `SchemaAnswer`: questionId, answer, notes
- `SchemaMapping`: cevaplar + tablo özeti
- `QuestionPriority` enum: Low=0, Medium=1, High=2, Critical=3

### Otomatik Tablo Tespiti:
- **Satış:** sale, satis, invoice, fatura, order, siparis
- **Ürün:** product, urun, item, kitap, book, stock, stok
- **Mağaza:** store, magaza, branch, sube, shop

---

## BÖLÜM 2 — ONBOARDING SAYFASI

**Dosya:** `yonetiq/Components/Pages/Admin/SchemaOnboarding.razor`

**Route:** `/admin/schema-onboarding`

### 4 Adımlı Wizard:
1. **Kaynak Seç** — Aktif DataSource'lardan birini seç
2. **Keşfet** — `DiscoverAsync()` çalıştır (spinner)
3. **Soruları Cevapla** — Kritik sorular cevaplandığında devam
4. **Onayla** — Özet tablo, kaydet butonu

### Kurallar:
- Sadece Admin/Yönetici erişebilir
- Kritik sorular cevaplamadan devam edilemez
- Kayıt `schema_mapping.json`'a yapılır

---

## BÖLÜM 3 — SCHEMA MAPPING → SEMANTIC DEFINITIONS KÖPRÜSÜ

**Dosya:** `yonetiq/Data/Services/AI/SemanticBootstrapService.cs`

`schema_mapping.json`'daki cevaplara bakarak `SemanticDefinitions` tablosunu otomatik bootstrap eder.

### Üretilen Terimler:
- **Ölçüler:** ciro (SUM), satış adedi (SUM)
- **Tarih aralıkları:** geçen ay, bu ay, geçen yıl, bu yıl, son 30 gün
- **Boyutlar:** mağaza, kategori
- **KPI:** büyüme oranı

### Kritik Fark:
Tablo ve kolon adları `schema_mapping.json`'dan gelir — hardcoded tahmin yok.

---

## BÖLÜM 4 — CLAUDE CODE SESSION PROTOKOLÜ

### Oturum Açılışı:
1. Dosyaları oku: UNIFIED_MASTER, FINAL_PRECODE, schema_mapping.json (varsa), task.md
2. `schema_mapping.json` kontrolü:
   - VAR → şema biliniyor, devam et
   - YOK → Fikri'ye sor, onboarding yönlendir
3. Şema bilinmiyorsa: SemanticDefinitions'a BKM terimi YAZMA, tablo adı KULLANMA, NlToSql testi YAPMA
4. Şema biliniyorsa: schema_mapping.json'daki gerçek adları kullan

---

## BÖLÜM 5 — DI KAYITLARI VE NAVİGASYON

```csharp
// Program.cs
builder.Services.AddScoped<SchemaDiscoveryService>();
builder.Services.AddScoped<SemanticBootstrapService>();
```

```html
<!-- NavMenu.razor — Admin menüsüne ekle -->
<NavLink href="/admin/schema-onboarding">
    <i class="bi bi-diagram-3"></i> Şema Kurulumu
</NavLink>
```

---

## BÖLÜM 6 — TAM UYGULAMA SIRASI (58 Adım)

### Faz 0 — Hazırlık (1-7)
1. git init + .gitignore + .env ✅
2. SchemaDiscoveryService + SemanticBootstrapService yaz
3. /admin/schema-onboarding sayfası yaz
4. Uygulamayı çalıştır → onboarding'e git → schema_mapping.json oluştur
5. [DURAK: Fikri schema_mapping.json'ı onaylar]
6. SemanticBootstrapService çalıştır → SemanticDefinitions dolar
7. sp_DataQuality_Check çalıştır, anomalileri not al

### Faz 1-9 (8-58)
YONETIQ_FINAL_PRECODE.md'deki 55 adım (gerçek tablo/kolon adlarıyla)
